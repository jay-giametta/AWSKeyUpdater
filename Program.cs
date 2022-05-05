using Amazon;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace AWSKeyUpdater
{
    class Program
    {
        protected static IAmazonIdentityManagementService identityManagementService;

        static async Task Main(string[] args)
        {
            bool loginStatus = false;
            while (!loginStatus)
            {
                if (await GetCredentials() == HttpStatusCode.OK)
                {
                    loginStatus = true;
                }
                else
                {
                    Console.Write("Login Failed\n\nPress any key to continue...");
                    Console.ReadKey();
                }
            }
            await MainMenu();        
        }

        protected async static Task MainMenu()
        {
            Console.Clear();
            Console.WriteLine("1. List Access Keys");
            Console.WriteLine("2. Create An Access Key");
            Console.WriteLine("3. Delete An Access Key");
            Console.WriteLine("4. Exit\n");
            Console.Write("Select An Option: ");
            ConsoleKeyInfo input = Console.ReadKey();
            if(input.Key == ConsoleKey.Escape)
            {
                return;
            }
            switch (input.KeyChar)
            {
                case '1':
                    await ListMenu();
                    await MainMenu();
                    break;
                case '2':
                    await CreateMenu();
                    await MainMenu();
                    break;
                case '3':
                    await DeleteMenu();
                    await MainMenu();
                    break;
                case '4':
                    break;
                default:
                    InvalidSelection();
                    await MainMenu();
                    break;
            }
        }

        protected async static Task ListMenu()
        {
            Console.Clear();
            try
            {
                List<AccessKeyMetadata> accessKeyMetadata = await ListAccessKeys();
                Console.WriteLine("Access keys:");
                for (int i = 0; i < accessKeyMetadata.Count(); i++)
                {
                    Console.WriteLine($"{i}: Key: {accessKeyMetadata[i].AccessKeyId}, User: {accessKeyMetadata[i].UserName}, Date: {accessKeyMetadata[i].CreateDate}");
                }
            }
            catch (Exception exception)
            {
                Console.Clear();
                Console.WriteLine(exception.Message);
            }
            Console.Write("\nPress any key to continue...");
            Console.ReadKey();
        }

        protected async static Task CreateMenu()
        {
            Console.Clear();
            try
            {
                AccessKey accessKey = await CreateAccessKey();
                Console.WriteLine("Access key created:");
                Console.WriteLine($"Id: {accessKey.AccessKeyId}\nSecret: {accessKey.SecretAccessKey}");
            }
            catch (Exception exception)
            {
                Console.Clear();
                Console.WriteLine(exception.Message);
            }
            Console.Write("\nPress any key to continue...");
            Console.ReadKey();
        }

        protected async static Task DeleteMenu()
        {
            Console.Clear();
            try
            {
                bool done = false;

                while (!done)
                {
                    List<AccessKeyMetadata> accessKeyMetadata = await ListAccessKeys();
                    Console.WriteLine("Access keys:");
                    for (int i = 0; i < accessKeyMetadata.Count(); i++)
                    {
                        Console.WriteLine($"{i + 1}: Key: {accessKeyMetadata[i].AccessKeyId}, User: {accessKeyMetadata[i].UserName}, Date: {accessKeyMetadata[i].CreateDate}");
                    }
                    Console.Write("\nSelect a key to delete (Press Esc to return to the main menu): ");
                    ConsoleKeyInfo input = Console.ReadKey();
                    if(input.Key == ConsoleKey.Escape)
                    {
                        done = true;
                    }
                    else if (!int.TryParse(input.KeyChar.ToString(), out int option) || option < 1 || option > accessKeyMetadata.Count())
                    {
                        InvalidSelection();
                    }
                    else
                    {
                        char verify = ' ';
                        while (verify != 'y' && verify != 'n')
                        {
                            Console.Clear();
                            Console.WriteLine($"Delete Key: {accessKeyMetadata[option - 1].AccessKeyId}, User: {accessKeyMetadata[option - 1].UserName}, Date: {accessKeyMetadata[option - 1].CreateDate}?");
                            Console.Write("\nChoose [y]es or [n]o: ");
                            verify = Console.ReadKey().KeyChar;
                        }

                        if (verify == 'y')
                        {
                            await DeleteAccessKey(accessKeyMetadata[option - 1].AccessKeyId);
                            Console.Clear();
                            Console.WriteLine("Access key deleted:");
                            Console.WriteLine($"Id: {accessKeyMetadata[option - 1].AccessKeyId}");
                            Console.Write("\nPress any key to continue...");
                            Console.ReadKey();
                            done = true;
                        }
                    }
                }           
            }
            catch (Exception exception)
            {
                Console.Clear();
                Console.WriteLine(exception.Message);
                Console.Write("\nPress any key to continue...");
                Console.ReadKey();
            }
        }

        protected static IAmazonIdentityManagementService GetIdentityManagementService(string awsAccessKeyId, string awsSecretAccessKey, string awsRegion)
        {
            return new AmazonIdentityManagementServiceClient(awsAccessKeyId, awsSecretAccessKey, RegionEndpoint.GetBySystemName(awsRegion));
        }

        protected async static Task<AccessKey> CreateAccessKey()
        {
            CreateAccessKeyResponse response = await identityManagementService.CreateAccessKeyAsync();
            return response.AccessKey;
        }

        protected async static Task<ResponseMetadata> DeleteAccessKey(string keyId)
        {
            DeleteAccessKeyRequest request = new()
            {
                AccessKeyId = keyId
            };

            DeleteAccessKeyResponse response = await identityManagementService.DeleteAccessKeyAsync(request);
            return response.ResponseMetadata;
        }

        protected async static Task<List<AccessKeyMetadata>> ListAccessKeys()
        {

            ListAccessKeysResponse response = await identityManagementService.ListAccessKeysAsync();
            return response.AccessKeyMetadata.ToList();
        }

        protected static void InvalidSelection()
        {
            Console.Clear();
            Console.WriteLine("Invalid selection");
            Console.Write("\nPress any key to continue...");
            Console.ReadKey();
        }

        protected async static Task<HttpStatusCode> GetCredentials()
        {
            const string defaultRegion = "us-gov-west-1";
            Console.Clear();
            Console.Write($"AWS Region [{defaultRegion}]: ");
            string awsRegion = Console.ReadLine();
            awsRegion = awsRegion == "" ? defaultRegion : awsRegion;
            Console.Clear();
            Console.Write("AWS Access Key ID: ");
            string awsAccessKeyId = Console.ReadLine();
            Console.Clear();
            Console.Write("AWS Secret Access Key: ");
            string awsSecretAccessKey = Console.ReadLine();
            Console.Clear();
            try
            {
                identityManagementService = GetIdentityManagementService(awsAccessKeyId, awsSecretAccessKey, awsRegion);
                ListAccessKeysResponse response = await identityManagementService.ListAccessKeysAsync();
                return response.HttpStatusCode;
            }
            catch (Exception)
            {
                return HttpStatusCode.BadRequest;
            }
        }
    }
}
