﻿using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;

using Trivial.Web;

namespace Trivial.Security
{
    /// <summary>
    /// Json web token model.
    /// </summary>
    /// <typeparam name="T">The type of payload.</typeparam>
    public class JsonWebToken<T>
    {
        /// <summary>
        /// Parses a JWT string encoded.
        /// </summary>
        /// <param name="jwt">The string encoded.</param>
        /// <param name="algorithm">The signature algorithm.</param>
        /// <param name="verify">true if verify the signature; otherwise, false.</param>
        /// <returns>A JSON web token object.</returns>
        /// <exception cref="ArgumentNullException">jwt was null or empty. -or- algorithm was null and verify is true.</exception>
        /// <exception cref="ArgumentException">jwt did not contain any information.</exception>
        /// <exception cref="FormatException">jwt was in incorrect format.</exception>
        /// <exception cref="InvalidOperationException">Verify failure.</exception>
        public static JsonWebToken<T> Parse(string jwt, ISignatureProvider algorithm, bool verify = true)
        {
            if (string.IsNullOrWhiteSpace(jwt)) throw new ArgumentNullException(nameof(jwt), "jwt should not be null or empty.");
            var prefix = $"{TokenInfo.BearerTokenType} ";
            if (jwt.IndexOf(prefix) == 0)
            {
                if (jwt.Length == prefix.Length) throw new ArgumentException("jwt should not contain a scheme only.", nameof(jwt));
                jwt = jwt.Substring(prefix.Length);
            }

            var arr = jwt.Split('.');
            if (arr.Length < 3) throw new FormatException("jwt is not in the correct format.");
            if (verify)
            {
                var bytes = Encoding.ASCII.GetBytes($"{arr[0]}.{arr[1]}");
                var sign = WebUtility.Base64UrlDecode(arr[2]);
                if (algorithm != null)
                {
                    if (!algorithm.Verify(bytes, sign)) throw new InvalidOperationException("jwt signature is incorrect.");
                }
                else
                {
                    if (sign.Length > 0) throw new ArgumentNullException(nameof(algorithm), "algorithm should not be null.");
                }
            }

            var header = WebUtility.Base64UrlDecodeTo<JsonWebTokenHeader>(arr[0]);
            var payload = WebUtility.Base64UrlDecodeTo<T>(arr[1]);
            if (header == null) throw new ArgumentException("jwt should contain header in Base64Url.", nameof(jwt));
            if (payload == null) throw new ArgumentException("jwt should contain payload in Base64Url.", nameof(jwt));
            var obj = new JsonWebToken<T>(payload, algorithm)
            {
                headerBase64Url = arr[0],
                signatureCache = arr[2]
            };
            obj.header.Type = header.Type;
            obj.header.AlgorithmName = header.AlgorithmName;
            return obj;
        }

        /// <summary>
        /// Parses a JWT string encoded.
        /// </summary>
        /// <param name="token">The access token.</param>
        /// <param name="algorithm">The signature algorithm.</param>
        /// <param name="verify">true if verify the signature; otherwise, false.</param>
        /// <returns>A JSON web token object.</returns>
        /// <exception cref="ArgumentNullException">token was null. -or- algorithm was null and verify is true.</exception>
        /// <exception cref="ArgumentException">token is not a Bearer token, or its access token did not contain the required information.</exception>
        /// <exception cref="FormatException">The access token was in incorrect format.</exception>
        /// <exception cref="InvalidOperationException">Verify failure.</exception>
        public static JsonWebToken<T> Parse(TokenInfo token, ISignatureProvider algorithm, bool verify = true)
        {
            if (token == null) throw new ArgumentNullException(nameof(token), "token should not be null.");
            if (string.IsNullOrWhiteSpace(token.AccessToken)) throw new ArgumentException("The access token should not be null or empty.", nameof(token));
            if (!string.IsNullOrEmpty(token.TokenType) && token.TokenType != TokenInfo.BearerTokenType) throw new ArgumentException("The token type should be Bearer.", nameof(token));
            try
            {
                return Parse(token.AccessToken, algorithm, verify);
            }
            catch (ArgumentException ex)
            {
                var msg = string.IsNullOrWhiteSpace(ex.Message) || ex.Message.Length < 7 || ex.Message.IndexOf("jwt ") != 0
                    ? "The access token is incorrect."
                    : ("The access token" + ex.Message.Substring(3));
                throw new ArgumentException(msg, nameof(token), ex);
            }
        }

        /// <summary>
        /// The signature algorithm function.
        /// </summary>
        private readonly ISignatureProvider signature;

        /// <summary>
        /// The Jwt header model.
        /// </summary>
        private readonly JsonWebTokenHeader header;

        /// <summary>
        /// The header Base64Url encoded string.
        /// </summary>
        private string headerBase64Url;

        /// <summary>
        /// The signature cache.
        /// </summary>
        private string signatureCache;

        /// <summary>
        /// Initializes a new instance of the JwtModel class.
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <param name="sign">The signature provider.</param>
        public JsonWebToken(T payload, ISignatureProvider sign)
        {
            Payload = payload;
            signature = sign;
            header = new JsonWebTokenHeader
            {
                AlgorithmName = sign != null ? sign.Name : "none"
            };
        }

        /// <summary>
        /// Gets the header.
        /// </summary>
        public string AlgorithmName => header.AlgorithmName;

        /// <summary>
        /// Gets the payload.
        /// </summary>
        public T Payload { get; }

        /// <summary>
        /// Gets the Base64Url of payload.
        /// </summary>
        /// <returns>A string encoded of payload.</returns>
        public string ToPayloadBase64Url()
        {
            return WebUtility.Base64UrlEncode(Payload);
        }

        /// <summary>
        /// Gets the Base64Url of header.
        /// </summary>
        /// <returns>A string encoded of header.</returns>
        public string ToHeaderBase64Url()
        {
            if (headerBase64Url == null) headerBase64Url = WebUtility.Base64UrlEncode(header);
            return headerBase64Url;
        }

        /// <summary>
        /// Gets the Base64Url of signature.
        /// </summary>
        /// <returns>A string encoded of signature.</returns>
        public string ToSigntureBase64Url()
        {
            if (signature == null || !signature.CanSign) return signatureCache ?? string.Empty;
            var bytes = signature.Sign($"{ToHeaderBase64Url()}.{ToPayloadBase64Url()}");
            return WebUtility.Base64UrlEncode(bytes);
        }

        /// <summary>
        /// Gets the encoded string.
        /// </summary>
        /// <returns>A string encoded.</returns>
        public string ToEncodedString()
        {
            return $"{ToHeaderBase64Url()}.{ToPayloadBase64Url()}.{ToSigntureBase64Url()}";
        }

        /// <summary>
        /// Returns a System.Net.Http.Headers.AuthenticationHeaderValue that represents the current TokenInfo.
        /// </summary>
        /// <returns>A System.Net.Http.Headers.AuthenticationHeaderValue that represents the current TokenInfo.</returns>
        public AuthenticationHeaderValue ToAuthenticationHeaderValue()
        {
            return new AuthenticationHeaderValue(TokenInfo.BearerTokenType, ToEncodedString());
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            if (Payload == null) return string.Empty;
            return Payload.ToString();
        }
    }

    /// <summary>
    /// Json web token header model.
    /// </summary>
    [DataContract]
    public class JsonWebTokenHeader
    {
        /// <summary>
        /// Gets or sets the name of signature algorithm.
        /// </summary>
        [DataMember(Name = "alg")]
        public string AlgorithmName { get; set; }

        /// <summary>
        /// Gets the type.
        /// </summary>
        [DataMember(Name = "typ")]
        public string Type { get; set; } = "JWT";
    }

    /// <summary>
    /// The base JWT payload model.
    /// </summary>
    [DataContract]
    public class JsonWebTokenPayload
    {
        /// <summary>
        /// Gets or sets the optional JWT ID.
        /// This claim provides a unique identifier for the JWT.
        /// The identifier value MUST be assigned in a manner that ensures that
        /// there is a negligible probability that the same value will be
        /// accidentally assigned to a different data object; if the application
        /// uses multiple issuers, collisions MUST be prevented among values
        /// produced by different issuers as well.
        /// This claim can be used to prevent the JWT from being replayed.
        /// Its value is a case-sensitive string.
        /// </summary>
        [DataMember(Name = "jti")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the optional issuer identifier.
        /// This claim identifies the principal that issued the
        /// JWT.The processing of this claim is generally application specific.
        /// Its value is a case-sensitive string containing a string or a URI.
        /// </summary>
        [DataMember(Name = "iss")]
        public string Issuer { get; set; }

        /// <summary>
        /// Gets or sets the optional subject.
        /// This claim identifies the principal that is the
        /// subject of the JWT.The claims in a JWT are normally statements
        /// about the subject.
        /// The subject value MUST either be scoped to be locally unique
        /// in the context of the issuer or be globally unique.
        /// The processing of this claim is generally application specific.
        /// Its value is a case-sensitive string containing a string or a URI.
        /// </summary>
        [DataMember(Name = "sub")]
        public string Subject { get; set; }

        /// <summary>
        /// Gets or sets the optional audience identifier.
        /// This claim identifies the recipients that the JWT is
        /// intended for.  Each principal intended to process the JWT MUST
        /// identify itself with a value in the audience claim.If the principal
        /// processing the claim does not identify itself with a value in the
        /// this claim when this claim is present, then the JWT MUST be
        /// rejected.
        /// In the general case, its value is an array of case-sensitive strings,
        /// each containing a string or a URI.
        /// In the special case when the JWT has one audience, its value MAY be a
        /// single case-sensitive string containing a string or a URI. The
        /// interpretation of audience values is generally application specific
        /// </summary>
        [DataMember(Name = "aud")]
        public IEnumerable<string> Audience { get; set; }

        /// <summary>
        /// Gets or sets an optional expiration date time tick in JSON format.
        /// The original property is Expiration.
        /// </summary>
        [DataMember(Name = "exp")]
        public long? ExpirationTick
        {
            get
            {
                return WebUtility.ParseDate(Expiration);
            }

            set
            {
                Expiration = WebUtility.ParseDate(value);
            }
        }

        /// <summary>
        /// Gets or sets an optional expiration date time.
        /// This claim identifies the expiration time on or after
        /// which the JWT MUST NOT be accepted for processing.
        /// The processing of this claim requires that the current date time
        /// MUST be before the expiration date time listed in this claim.
        /// Implementers MAY provide for some small leeway,
        /// usually no more than a few minutes, to account for clock skew.
        /// </summary>
        public DateTime? Expiration { get; set; }

        /// <summary>
        /// Gets or sets an optional available start date time tick in JSON format.
        /// The original property is NotBefore.
        /// </summary>
        [DataMember(Name = "nbf")]
        public long? NotBeforeTick
        {
            get
            {
                return WebUtility.ParseDate(NotBefore);
            }

            set
            {
                NotBefore = WebUtility.ParseDate(value);
            }
        }

        /// <summary>
        /// Gets or sets an optional available start date time.
        /// This claim identifies the time before which the JWT
        /// MUST NOT be accepted for processing.
        /// The processing of this claim requires that the current date time
        /// MUST be after or equal to the not-before date time listed in this claim.
        /// Implementers MAY provide for some small leeway,
        /// usually no more than a few minutes, to account for clock skew.
        /// </summary>
        public DateTime? NotBefore { get; set; }

        /// <summary>
        /// Gets or sets an optional issue creation date time tick in JSON format.
        /// The original property is IssuedAt.
        /// </summary>
        [DataMember(Name = "iat")]
        public long? IssuedAtTick
        {
            get
            {
                return WebUtility.ParseDate(IssuedAt);
            }

            set
            {
                IssuedAt = WebUtility.ParseDate(value);
            }
        }

        /// <summary>
        /// Gets or sets an optional issue creation date time.
        /// This claim identifies the time at which the JWT was issued.
        /// This claim can be used to determine the age of the JWT.
        /// </summary>
        public DateTime? IssuedAt { get; set; }

        /// <summary>
        /// Returns a System.Net.Http.Headers.AuthenticationHeaderValue that represents the current token.
        /// </summary>
        /// <param name="sign">The signature provider.</param>
        /// <returns>A System.Net.Http.Headers.AuthenticationHeaderValue that represents the current token.</returns>
        public AuthenticationHeaderValue ToAuthenticationHeaderValue(ISignatureProvider sign)
        {
            var jwt = new JsonWebToken<JsonWebTokenPayload>(this, sign);
            return jwt.ToAuthenticationHeaderValue();
        }
    }
}
