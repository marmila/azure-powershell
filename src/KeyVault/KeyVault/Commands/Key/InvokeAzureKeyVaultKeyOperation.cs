﻿using Microsoft.Azure.Commands.Common.Exceptions;
using Microsoft.Azure.Commands.KeyVault.Models;
using Microsoft.Azure.Commands.ResourceManager.Common.ArgumentCompleters;
using Microsoft.WindowsAzure.Commands.Common.CustomAttributes;
using Microsoft.WindowsAzure.Commands.Utilities.Common;

using System;
using System.Collections;
using System.Management.Automation;
using System.Security;
using System.Text;

namespace Microsoft.Azure.Commands.KeyVault.Commands.Key
{
    /// <summary>
    /// 1. Encrypts an arbitrary sequence of bytes using an encryption key that is stored in a key vault.
    /// 2. Decrypts a single block of encrypted data.
    /// 3. Wraps a symmetric key using a specified key.
    /// 4. Unwraps a symmetric key using the specified key that was initially used for wrapping that key.
    /// </summary>
    [CmdletOutputBreakingChangeWithVersion(typeof(PSKeyOperationResult), "12.0.0", "6.0.0", DeprecatedOutputProperties = new string[] { "Result" }, NewOutputProperties = new string[] { "RawResult" })]
    [Cmdlet(VerbsLifecycle.Invoke, ResourceManager.Common.AzureRMConstants.AzurePrefix + "KeyVaultKeyOperation", SupportsShouldProcess = true, DefaultParameterSetName = ByVaultNameParameterSet)]
    [OutputType(typeof(PSKeyOperationResult))]
    public class InvokeAzureKeyVaultKeyOperation : KeyVaultKeyCmdletBase
    {
        #region Supported Operation 
        enum Operations
        {
            Unknown,
            Encrypt,
            Decrypt,
            Wrap,
            Unwrap
        }
        #endregion

        #region Input Parameter Definitions

        /// <summary>
        /// Key version.
        /// </summary>
        [Parameter(Mandatory = false,
            HelpMessage = "Key version.")]
        [Alias("KeyVersion")]
        public string Version { get; set; }

        [Parameter(Mandatory = true,
            HelpMessage = "Algorithm identifier")]
        [ValidateNotNullOrEmpty]
        [PSArgumentCompleter("Encrypt", "Decrypt", "Wrap", "Unwrap")]
        public string Operation { get; set; }

        [Parameter(Mandatory = true,
            HelpMessage = "Algorithm identifier")]
        [ValidateNotNullOrEmpty]
        [PSArgumentCompleter("RSA-OAEP", "RSA-OAEP-256", "RSA1_5")]
        [Alias("EncryptionAlgorithm", "WrapAlgorithm")]
        public string Algorithm { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "The value to be operated. This parameter will be converted to byte array in UTF-8 encoding way. If your value can't be encoded by UTF-8, please use parameter ByteArrayValue as its alternative.")]
        [ValidateNotNullOrEmpty]
        [CmdletParameterBreakingChangeWithVersion(nameof(Value), "12.0.0", "6.0.0", ReplaceMentCmdletParameterName = nameof(ByteArrayValue))]
        public SecureString Value { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "The value to be operated in byte array format.")]
        [ValidateNotNullOrEmpty]
        public byte[] ByteArrayValue { get; set; }

        #endregion Input Parameter Definitions

        private Operations opt = Operations.Unknown;

        internal void ValidateParameters()
        {
            if (this.IsParameterBound(c => c.Value) && this.IsParameterBound(c => c.ByteArrayValue))
            {
                throw new AzPSArgumentException(string.Format("Please provide only one of parameter Value and ByteArrayValue"), nameof(ByteArrayValue));
            }
            else if (!this.IsParameterBound(c => c.Value) && !this.IsParameterBound(c => c.ByteArrayValue))
            {
                throw new AzPSArgumentException(string.Format("Must provide one of parameter Value and ByteArrayValue"), nameof(ByteArrayValue));
            }
        }

        internal override void NormalizeParameterSets()
        {

            if (InputObject != null)
            {
                Version = Version ?? InputObject.Version;
            }

            Enum.TryParse(Operation, out opt);

            if (this.IsParameterBound(c => c.Value))
            {
                switch (opt)
                {
                    case Operations.Encrypt:
                        ByteArrayValue = Encoding.UTF8.GetBytes(Value.ConvertToString()); 
                        break;
                    case Operations.Decrypt:
                        ByteArrayValue = Convert.FromBase64String(Value.ConvertToString()); 
                        break;
                    case Operations.Wrap:
                        ByteArrayValue = Encoding.UTF8.GetBytes(Value.ConvertToString());
                        break;
                    case Operations.Unwrap:
                        ByteArrayValue = Convert.FromBase64String(Value.ConvertToString());
                        break;
                    default:
                        throw new NotSupportedException("Not supported ${Operation} yet");
                }
            }

            base.NormalizeParameterSets();
        }
        
        public override void ExecuteCmdlet()
        {
            ValidateParameters();
            NormalizeParameterSets();

            if (string.IsNullOrEmpty(HsmName))
            {
                switch (opt)
                {
                    case Operations.Encrypt:
                        this.WriteObject(
                            this.Track2DataClient.Encrypt(VaultName, Name, Version, ByteArrayValue, Algorithm));
                        break;
                    case Operations.Decrypt:
                        this.WriteObject(
                            this.Track2DataClient.Decrypt(VaultName, Name, Version, ByteArrayValue, Algorithm));
                        break;
                    case Operations.Wrap:
                        this.WriteObject(
                            this.Track2DataClient.WrapKey(VaultName, Name, Version, ByteArrayValue, Algorithm));
                        break;
                    case Operations.Unwrap:
                        this.WriteObject(
                            this.Track2DataClient.UnwrapKey(VaultName, Name, Version, ByteArrayValue, Algorithm));
                        break;
                    case Operations.Unknown:
                        throw new NotSupportedException("Not supported ${Operation} yet");
                }
            }
            else
            {
                switch (opt)
                {
                    case Operations.Encrypt:
                        this.WriteObject(
                            this.Track2DataClient.ManagedHsmKeyEncrypt(HsmName, Name, Version, ByteArrayValue, Algorithm));
                        break;
                    case Operations.Decrypt:
                        this.WriteObject(
                            this.Track2DataClient.ManagedHsmKeyDecrypt(HsmName, Name, Version, ByteArrayValue, Algorithm));
                        break;
                    case Operations.Wrap:
                        this.WriteObject(
                            this.Track2DataClient.ManagedHsmWrapKey(HsmName, Name, Version, ByteArrayValue, Algorithm));
                        break;
                    case Operations.Unwrap:
                        this.WriteObject(
                            this.Track2DataClient.ManagedHsmUnwrapKey(HsmName, Name, Version, ByteArrayValue, Algorithm));
                        break;
                    case Operations.Unknown:
                        throw new NotSupportedException("Not supported ${Operation} yet");
                }
                
            }
        }
    }
}
