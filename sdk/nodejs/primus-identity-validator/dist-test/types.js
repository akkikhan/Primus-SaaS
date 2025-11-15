"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ValidationMode = void 0;
/**
 * Validation mode for JWT token validation
 */
var ValidationMode;
(function (ValidationMode) {
    /**
     * Local JWT validation using symmetric key (HMAC)
     */
    ValidationMode["Local"] = "Local";
    /**
     * Azure AD token validation using asymmetric keys (RSA)
     */
    ValidationMode["AzureAd"] = "AzureAd";
    /**
     * Hybrid mode - try Azure AD first, fallback to Local
     */
    ValidationMode["Hybrid"] = "Hybrid";
})(ValidationMode || (exports.ValidationMode = ValidationMode = {}));
