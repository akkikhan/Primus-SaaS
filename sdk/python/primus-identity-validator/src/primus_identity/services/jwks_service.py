"""
JWKS (JSON Web Key Set) cache and service for fetching signing keys.
"""

from __future__ import annotations

import asyncio
import hashlib
import time
from typing import Any, Dict, List, Optional

import httpx
from cachetools import TTLCache
from cryptography.hazmat.primitives.asymmetric.rsa import RSAPublicKey
from jwt import PyJWK


class JwksCache:
    """Thread-safe cache for JWKS data with TTL expiration."""

    def __init__(self, ttl_hours: int = 24, max_size: int = 100) -> None:
        """Initialize the JWKS cache.

        Args:
            ttl_hours: Time-to-live for cache entries in hours.
            max_size: Maximum number of cached entries.
        """
        self._ttl_seconds = ttl_hours * 3600
        self._cache: TTLCache[str, Dict[str, PyJWK]] = TTLCache(
            maxsize=max_size, ttl=self._ttl_seconds
        )
        self._lock = asyncio.Lock()

    def _get_cache_key(self, jwks_uri: str) -> str:
        """Generate a cache key from the JWKS URI."""
        return hashlib.sha256(jwks_uri.encode()).hexdigest()

    async def get(self, jwks_uri: str) -> Optional[Dict[str, PyJWK]]:
        """Get cached JWKS data.

        Args:
            jwks_uri: The JWKS endpoint URI.

        Returns:
            Cached JWKS keys mapped by kid, or None if not cached.
        """
        async with self._lock:
            cache_key = self._get_cache_key(jwks_uri)
            return self._cache.get(cache_key)

    async def set(self, jwks_uri: str, keys: Dict[str, PyJWK]) -> None:
        """Cache JWKS data.

        Args:
            jwks_uri: The JWKS endpoint URI.
            keys: The JWKS keys mapped by kid.
        """
        async with self._lock:
            cache_key = self._get_cache_key(jwks_uri)
            self._cache[cache_key] = keys

    def clear(self) -> None:
        """Clear all cached entries."""
        self._cache.clear()


class JwksService:
    """Service for fetching and managing JWKS (JSON Web Key Sets)."""

    def __init__(self, cache: JwksCache, http_client: Optional[httpx.AsyncClient] = None) -> None:
        """Initialize the JWKS service.

        Args:
            cache: The JWKS cache instance.
            http_client: Optional HTTP client (created if not provided).
        """
        self._cache = cache
        self._http_client = http_client
        self._owns_client = http_client is None

    async def _get_client(self) -> httpx.AsyncClient:
        """Get or create the HTTP client."""
        if self._http_client is None:
            self._http_client = httpx.AsyncClient(timeout=30.0)
        return self._http_client

    async def get_signing_key(self, jwks_uri: str, kid: Optional[str] = None) -> PyJWK:
        """Get a signing key from the JWKS endpoint.

        Args:
            jwks_uri: The JWKS endpoint URI.
            kid: Optional key ID to find a specific key.

        Returns:
            The PyJWK signing key.

        Raises:
            ValueError: If no matching key is found.
        """
        # Check cache first
        cached_keys = await self._cache.get(jwks_uri)
        if cached_keys:
            if kid and kid in cached_keys:
                return cached_keys[kid]
            elif not kid and cached_keys:
                # Return first signing key
                for key in cached_keys.values():
                    return key

        # Fetch from endpoint
        keys = await self._fetch_jwks(jwks_uri)
        await self._cache.set(jwks_uri, keys)

        if kid:
            if kid in keys:
                return keys[kid]
            raise ValueError(f"Key with kid '{kid}' not found in JWKS")

        if keys:
            # Return first key
            return next(iter(keys.values()))

        raise ValueError("No signing keys found in JWKS")

    async def _fetch_jwks(self, jwks_uri: str) -> Dict[str, PyJWK]:
        """Fetch JWKS from the endpoint.

        Args:
            jwks_uri: The JWKS endpoint URI.

        Returns:
            Dictionary mapping kid to PyJWK objects.
        """
        client = await self._get_client()
        response = await client.get(jwks_uri)
        response.raise_for_status()

        jwks_data = response.json()
        keys: Dict[str, PyJWK] = {}

        for key_data in jwks_data.get("keys", []):
            # Only include signing keys
            if key_data.get("use", "sig") != "sig":
                continue

            kid = key_data.get("kid", "default")
            try:
                jwk = PyJWK.from_dict(key_data)
                keys[kid] = jwk
            except Exception:
                # Skip keys that can't be parsed
                continue

        return keys

    async def close(self) -> None:
        """Close the HTTP client if owned by this service."""
        if self._owns_client and self._http_client is not None:
            await self._http_client.aclose()
            self._http_client = None


class OpenIdConfigurationService:
    """Service for fetching OpenID Connect discovery documents."""

    WELL_KNOWN_PATH = "/.well-known/openid-configuration"

    def __init__(
        self, cache_ttl_hours: int = 24, http_client: Optional[httpx.AsyncClient] = None
    ) -> None:
        """Initialize the OpenID configuration service.

        Args:
            cache_ttl_hours: TTL for cached configurations in hours.
            http_client: Optional HTTP client.
        """
        self._cache: TTLCache[str, Dict[str, Any]] = TTLCache(
            maxsize=50, ttl=cache_ttl_hours * 3600
        )
        self._http_client = http_client
        self._owns_client = http_client is None
        self._lock = asyncio.Lock()

    async def _get_client(self) -> httpx.AsyncClient:
        """Get or create the HTTP client."""
        if self._http_client is None:
            self._http_client = httpx.AsyncClient(timeout=30.0)
        return self._http_client

    async def get_configuration(self, authority: str) -> Dict[str, Any]:
        """Get the OpenID configuration for an authority.

        Args:
            authority: The OIDC authority URL.

        Returns:
            The OpenID configuration document.
        """
        cache_key = authority.rstrip("/")

        async with self._lock:
            if cache_key in self._cache:
                return self._cache[cache_key]

        # Fetch configuration
        config = await self._fetch_configuration(authority)

        async with self._lock:
            self._cache[cache_key] = config

        return config

    async def _fetch_configuration(self, authority: str) -> Dict[str, Any]:
        """Fetch OpenID configuration from the discovery endpoint.

        Args:
            authority: The OIDC authority URL.

        Returns:
            The OpenID configuration document.
        """
        # Build discovery URL
        base_url = authority.rstrip("/")
        discovery_url = f"{base_url}{self.WELL_KNOWN_PATH}"

        client = await self._get_client()
        response = await client.get(discovery_url)
        response.raise_for_status()

        return response.json()

    async def get_jwks_uri(self, authority: str) -> str:
        """Get the JWKS URI for an authority.

        Args:
            authority: The OIDC authority URL.

        Returns:
            The JWKS endpoint URI.
        """
        config = await self.get_configuration(authority)
        jwks_uri = config.get("jwks_uri")

        if not jwks_uri:
            raise ValueError(f"No jwks_uri found in OpenID configuration for {authority}")

        return jwks_uri

    async def close(self) -> None:
        """Close the HTTP client if owned by this service."""
        if self._owns_client and self._http_client is not None:
            await self._http_client.aclose()
            self._http_client = None
