"""
Pytest configuration for Primus Identity Validator tests.
"""

import pytest


def pytest_configure(config: pytest.Config) -> None:
    """Configure pytest with custom markers."""
    config.addinivalue_line(
        "markers",
        "integration: marks tests as integration tests (may require network access)",
    )
    config.addinivalue_line(
        "markers",
        "slow: marks tests as slow running",
    )
