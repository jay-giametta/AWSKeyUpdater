using Amazon;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AWSKeyUpdater
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (CheckFileArgs(args))
            {
                IAmazonIdentityManagementService identityManagementService = GetIdentityManagementService(args[0], "default", "us-gov-west-1");

                switch (args[1])
                {
                    case "create":
                        AccessKey accessKey = await CreateAccessKey(identityManagementService);
                        Console.WriteLine("Access key created:");
                        Console.WriteLine($"Id: {accessKey.AccessKeyId}\nSecret: {accessKey.SecretAccessKey}");
                        break;
                    case "delete":
                        await DeleteAccessKey(identityManagementService, args[2]);
                        Console.WriteLine("Access key deleted:");
                        Console.WriteLine($"Id: {args[2]}");
                        break;
                    case "list":
                        IEnumerable<AccessKeyMetadata> accessKeyMetadata = await ListAccessKeys(identityManagementService);
                        Console.WriteLine("Access keys:");
                        foreach (AccessKeyMetadata data in accessKeyMetadata)
                        {
                            Console.WriteLine($"Key: {data.AccessKeyId}, User: {data.UserName}, Date: {data.CreateDate}");
                        }
                        break;
                    default:
                        ShowSyntax();
                        break;
                }
            }
        }

        protected static bool CheckFileArgs(string[] args)
        {
            if (args.Length < 2 || (args.Contains("delete") && args.Length < 3))
            {
                ShowSyntax();
                return false;
            }
            return true;
        }

        protected static IAmazonIdentityManagementService GetIdentityManagementService(string credentialFilePath, string profile, string regionName)
        {
            SharedCredentialsFile credentialsFile = new(credentialFilePath);
            credentialsFile.TryGetProfile(profile, out CredentialProfile credentialProfile);
            AWSCredentialsFactory.TryGetAWSCredentials(credentialProfile, credentialsFile, out AWSCredentials awsCredentials);
            RegionEndpoint regionEndpoint = RegionEndpoint.GetBySystemName(regionName);

            return new AmazonIdentityManagementServiceClient(awsCredentials, regionEndpoint);
        }

        protected async static Task<AccessKey> CreateAccessKey(IAmazonIdentityManagementService identityManagementService)
        {
            CreateAccessKeyResponse response = await identityManagementService.CreateAccessKeyAsync();
            return response.AccessKey;
        }

        protected async static Task<ResponseMetadata> DeleteAccessKey(IAmazonIdentityManagementService identityManagementService, string keyId)
        {
            DeleteAccessKeyRequest request = new()
            {
                AccessKeyId = keyId
            };

            DeleteAccessKeyResponse response = await identityManagementService.DeleteAccessKeyAsync(request);
            return response.ResponseMetadata;
        }

        protected async static Task<IEnumerable<AccessKeyMetadata>> ListAccessKeys(IAmazonIdentityManagementService identityManagementService)
        {

            ListAccessKeysResponse response = await identityManagementService.ListAccessKeysAsync();
            return response.AccessKeyMetadata;
        }

        protected static void ShowSyntax()
        {
            Console.WriteLine("Error: Missing Arguments");
            Console.WriteLine("Syntax: awskeyupdater <current credential file> <list|create|delete <access key id>>");
        }
    }
}
