﻿//-----------------------------------------------------------------------
// <copyright file="StorageCredentials.cs" company="Microsoft">
//    Copyright 2013 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage.Auth
{
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

#if WINDOWS_RT
    using Windows.Foundation.Metadata;
#endif

    /// <summary>
    /// Represents a set of credentials used to authenticate access to a Windows Azure storage account.
    /// </summary>
    public sealed class StorageCredentials
    {
        private UriQueryBuilder queryBuilder;

        /// <summary>
        /// A value indicating whether or not this StorageCredentials object is meant for HTTPS access only.
        /// </summary>
        private bool isHttpsOnly;

        /// <summary>
        /// Gets the associated shared access signature token for the credentials.
        /// </summary>
        /// <value>The shared access signature token.</value>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SAS", Justification = "Back compatibility.")]
        public string SASToken { get; private set; }

        /// <summary>
        /// Gets the associated account name for the credentials.
        /// </summary>
        /// <value>The account name.</value>
        public string AccountName { get; private set; }

        /// <summary>
        /// Gets the associated key name for the credentials.
        /// </summary>
        /// <value>The key name.</value>
        public string KeyName 
        { 
            get 
            { 
                return this.Key.KeyName; 
            } 
        }

        internal StorageAccountKey Key { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the credentials are for anonymous access.
        /// </summary>
        /// <value><c>true</c> if the credentials are for anonymous access; otherwise, <c>false</c>.</value>
        public bool IsAnonymous
        {
            get
            {
                return (this.SASToken == null) && (this.AccountName == null);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the credentials are a shared access signature token.
        /// </summary>
        /// <value><c>true</c> if the credentials are a shared access signature token; otherwise, <c>false</c>.</value>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SAS", Justification = "Back compatibility.")]
        public bool IsSAS
        {
            get
            {
                return (this.SASToken != null);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the credentials are a shared key.
        /// </summary>
        /// <value><c>true</c> if the credentials are a shared key; otherwise, <c>false</c>.</value>
        public bool IsSharedKey
        {
            get
            {
                return (this.SASToken == null) && (this.AccountName != null);
            }
        }

        /// <summary>
        /// Gets the value of the shared access signature token's <code>sig</code> parameter.
        /// </summary>
        public string SASSignature
        {
            get
            {
                if (this.IsSAS)
                {
                    return this.queryBuilder[Constants.QueryConstants.Signature];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageCredentials"/> class.
        /// </summary>
        public StorageCredentials()
        {
        }

#if !PORTABLE
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageCredentials"/> class with the specified account name and key value.
        /// </summary>
        /// <param name="accountName">A string that represents the name of the storage account.</param>
        /// <param name="keyValue">A string that represents the Base64-encoded account access key.</param>
        public StorageCredentials(string accountName, string keyValue)
            : this(accountName, keyValue, null)
        {
        }

#if WINDOWS_DESKTOP
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageCredentials"/> class with the specified account name and key value.
        /// </summary>
        /// <param name="accountName">A string that represents the name of the storage account.</param>
        /// <param name="keyValue">An array of bytes that represent the account access key.</param>
        public StorageCredentials(string accountName, byte[] keyValue)
            : this(accountName, keyValue, null)
        {
        }
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageCredentials"/> class with the specified account name, key value, and key name.
        /// </summary>
        /// <param name="accountName">A string that represents the name of the storage account.</param>
        /// <param name="keyValue">A string that represents the Base64-encoded account access key.</param>
        /// <param name="keyName">A string that represents the name of the key.</param>
        public StorageCredentials(string accountName, string keyValue, string keyName)
        {
            CommonUtility.AssertNotNullOrEmpty("accountName", accountName);

            this.AccountName = accountName;
            this.UpdateKey(keyValue, keyName);
        }

#if WINDOWS_DESKTOP
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageCredentials"/> class with the specified account name, key value, and key name.
        /// </summary>
        /// <param name="accountName">A string that represents the name of the storage account.</param>
        /// <param name="keyValue">An array of bytes that represent the account access key.</param>
        /// <param name="keyName">A string that represents the name of the key.</param>
        public StorageCredentials(string accountName, byte[] keyValue, string keyName)
        {
            CommonUtility.AssertNotNullOrEmpty("accountName", accountName);

            this.AccountName = accountName;
            this.UpdateKey(keyValue, keyName);
        }
#endif
#endif 

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageCredentials"/> class with the specified shared access signature token.
        /// </summary>
        /// <param name="sasToken">A string representing the shared access signature token.</param>
        public StorageCredentials(string sasToken)
        {
            CommonUtility.AssertNotNullOrEmpty("sasToken", sasToken);

            this.SASToken = sasToken;
            this.UpdateQueryBuilder();
        }

        /// <summary>
        /// Updates the key value for the credentials.
        /// </summary>
        /// <param name="keyValue">The key value, as a Base64-encoded string, to update.</param>
        public void UpdateKey(string keyValue)
        {
            this.UpdateKey(keyValue, null);
        }

#if WINDOWS_DESKTOP
        /// <summary>
        /// Updates the key value for the credentials.
        /// </summary>
        /// <param name="keyValue">The key value, as an array of bytes, to update.</param>
        public void UpdateKey(byte[] keyValue)
        {
            this.UpdateKey(keyValue, null);
        }
#endif

        /// <summary>
        /// Updates the key value and key name for the credentials.
        /// </summary>
        /// <param name="keyValue">The key value, as a Base64-encoded string, to update.</param>
        /// <param name="keyName">The key name to update.</param>
        public void UpdateKey(string keyValue, string keyName)
        {
            if (!this.IsSharedKey)
            {
                string errorMessage = string.Format(CultureInfo.CurrentCulture, SR.CannotUpdateKeyWithoutAccountKeyCreds);
                throw new InvalidOperationException(errorMessage);
            }

            CommonUtility.AssertNotNull("keyValue", keyValue);

            this.Key = new StorageAccountKey(keyName, Convert.FromBase64String(keyValue));
        }

#if WINDOWS_DESKTOP
        /// <summary>
        /// Updates the key value and key name for the credentials.
        /// </summary>
        /// <param name="keyValue">The key value, as an array of bytes, to update.</param>
        /// <param name="keyName">The key name to update.</param>
        public void UpdateKey(byte[] keyValue, string keyName)
        {
            if (!this.IsSharedKey)
            {
                string errorMessage = string.Format(CultureInfo.CurrentCulture, SR.CannotUpdateKeyWithoutAccountKeyCreds);
                throw new InvalidOperationException(errorMessage);
            }

            CommonUtility.AssertNotNull("keyValue", keyValue);

            this.Key = new StorageAccountKey(keyName, keyValue);
        }
#endif

        /// <summary>
        /// Updates the shared access signature (SAS) token value for storage credentials created with a shared access signature.
        /// </summary>
        /// <param name="sasToken">A string that specifies the SAS token value to update.</param>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SAS", Justification = "Back compatibility.")]
        public void UpdateSASToken(string sasToken)
        {
            if (!this.IsSAS)
            {
                string errorMessage = string.Format(CultureInfo.CurrentCulture, SR.CannotUpdateSasWithoutSasCreds);
                throw new InvalidOperationException(errorMessage);
            }

            CommonUtility.AssertNotNullOrEmpty("sasToken", sasToken);

            this.SASToken = sasToken;
            this.UpdateQueryBuilder();
        }
        
        /// <summary>
        /// Returns the account key for the credentials.
        /// </summary>
        /// <returns>An array of bytes that contains the key.</returns>
        public byte[] ExportKey()
        {
            return (byte[])this.Key.KeyValue.Clone();
        }

        /// <summary>
        /// Transforms a resource URI into a shared access signature URI, by appending a shared access token.
        /// </summary>
        /// <param name="resourceUri">A <see cref="System.Uri"/> object that represents the resource URI to be transformed.</param>
        /// <returns>A <see cref="System.Uri"/> object that represents the signature, including the resource URI and the shared access token.</returns>
#if WINDOWS_RT
        [DefaultOverload]
#endif
        public Uri TransformUri(Uri resourceUri)
        {
            CommonUtility.AssertNotNull("resourceUri", resourceUri);
#if WINDOWS_RT || PORTABLE || ASPNET_K
            if (this.isHttpsOnly && (string.CompareOrdinal(resourceUri.Scheme, "https") != 0))
#else
            if (this.isHttpsOnly && (string.CompareOrdinal(resourceUri.Scheme, Uri.UriSchemeHttps) != 0))
#endif
            {
                throw new ArgumentException(SR.CannotTransformNonHttpsUriWithHttpsOnlyCredentials);
            }

            if (this.IsSAS)
            {
                return this.queryBuilder.AddToUri(resourceUri);
            }
            else
            {
                return resourceUri;
            }
        }

        /// <summary>
        /// Transforms a resource URI into a shared access signature URI, by appending a shared access token.
        /// </summary>
        /// <param name="resourceUri">A <see cref="StorageUri"/> object that represents the resource URI to be transformed.</param>
        /// <returns>A <see cref="StorageUri"/> object that represents the signature, including the resource URI and the shared access token.</returns>
        public StorageUri TransformUri(StorageUri resourceUri)
        {
            CommonUtility.AssertNotNull("resourceUri", resourceUri);

            return new StorageUri(
                this.TransformUri(resourceUri.PrimaryUri),
                this.TransformUri(resourceUri.SecondaryUri));
        }

        /// <summary>
        /// Exports the value of the account access key to a Base64-encoded string.
        /// </summary>
        /// <returns>The account access key.</returns>
        public string ExportBase64EncodedKey()
        {
            StorageAccountKey localKey = this.Key;
            return (localKey.KeyValue == null) ? null : Convert.ToBase64String(localKey.KeyValue);
        }

        internal string ToString(bool exportSecrets)
        {
            if (this.IsSharedKey)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}={1};{2}={3}",
                    CloudStorageAccount.AccountNameSettingString,
                    this.AccountName,
                    CloudStorageAccount.AccountKeySettingString,
                    exportSecrets ? this.ExportBase64EncodedKey() : "[key hidden]");
            }

            if (this.IsSAS)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}={1}", CloudStorageAccount.SharedAccessSignatureSettingString, exportSecrets ? this.SASToken : "[signature hidden]");
            }

            return string.Empty;
        }

        /// <summary>
        /// Determines whether an other <see cref="StorageCredentials"/> object is equal to this one by comparing their SAS tokens, account names, key names, and key values.
        /// </summary>
        /// <param name="other">The <see cref="StorageCredentials"/> object to compare to this one.</param>
        /// <returns><c>true</c> if the two <see cref="StorageCredentials"/> objects are equal; otherwise, <c>false</c>.</returns>
        public bool Equals(StorageCredentials other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                return string.Equals(this.SASToken, other.SASToken) &&
                    string.Equals(this.AccountName, other.AccountName) &&
                    string.Equals(this.KeyName, other.KeyName) &&
                    string.Equals(this.ExportBase64EncodedKey(), other.ExportBase64EncodedKey());
            }
        }

        private void UpdateQueryBuilder()
        {
            this.queryBuilder = new UriQueryBuilder();
            this.isHttpsOnly = false;
            IDictionary<string, string> parameters = HttpWebUtility.ParseQueryString(this.SASToken);
            foreach (KeyValuePair<string, string> parameter in parameters)
            {
                if (string.CompareOrdinal(parameter.Key, Constants.QueryConstants.SignedProtocols) == 0 && string.CompareOrdinal(parameter.Value, "https") == 0)
                {
                    this.isHttpsOnly = true;
                }

                this.queryBuilder.Add(parameter.Key, parameter.Value);
            }

            this.queryBuilder.Add(Constants.QueryConstants.ApiVersion, Constants.HeaderConstants.TargetStorageVersion);
            
        }
    }
}
