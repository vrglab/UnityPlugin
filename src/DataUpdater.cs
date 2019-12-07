using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Debug = UnityEngine.Debug;

#pragma warning disable 0618
namespace ModIO
{
    /// <summary>Performs the operations necessary to update data from older versions of the plugin.</summary>
    public static class DataUpdater
    {
        /// <summary>Runs the update functionality depending on the lastRunVersion.</summary>
        public static void UpdateFromVersion(SimpleVersion lastRunVersion)
        {
            if(lastRunVersion < new SimpleVersion(2, 1))
            {
                Update_2_0_to_2_1_UserData();
            }
        }

        /// <summary>Generic object wrapper for retrieving JSON Data from files.</summary>
        [System.Serializable]
        private struct GenericJSONObject
        {
            [JsonExtensionData]
            public IDictionary<string, JToken> data;
        }

        // ---------[ 2019 ]---------
        /// <summary>Moves the data from the UserAuthenticationData and ModManager caches to UserAccountManagement.</summary>
        private static void Update_2_0_to_2_1_UserData()
        {
            Debug.Log("[mod.io] Attempting 2.0->2.1 UserData update.");

            // check if the file already exists
            byte[] fileData = UserDataStorage.ReadBinaryFile("users/default.user");
            if(fileData != null && fileData.Length > 0)
            {
                Debug.Log("[mod.io] Aborting UserData update. FileExists: \'users/default.user\' ["
                          + ValueFormatting.ByteCount(fileData.Length, null) + "]");
            }

            // update
            GenericJSONObject dataWrapper;
            LocalUser userData = new LocalUser();
            string filePath = null;

            // - copy enabled/subbed -
            filePath = ModManager.PERSISTENTDATA_FILEPATH;

            if(IOUtilities.TryReadJsonObjectFile(filePath, out dataWrapper))
            {
                if(dataWrapper.data.ContainsKey("subscribedModIds"))
                {
                    JArray array = dataWrapper.data["subscribedModIds"] as JArray;
                    if(array != null)
                    {
                        userData.subscribedModIds = array.ToObject<int[]>();
                    }
                }

                if(dataWrapper.data.ContainsKey("enabledModIds"))
                {
                    JArray array = dataWrapper.data["enabledModIds"] as JArray;
                    if(array != null)
                    {
                        userData.enabledModIds = array.ToObject<int[]>();
                    }
                }
            }

            // - copy UAD -
            filePath = UserAuthenticationData.FILE_LOCATION;

            if(IOUtilities.TryReadJsonObjectFile(filePath, out dataWrapper))
            {
                // user profile
                int userId = UserProfile.NULL_ID;
                if(dataWrapper.data.ContainsKey("userId"))
                {
                    userId = (int)dataWrapper.data["userId"];
                }

                userData.profile = null;
                if(userId != UserProfile.NULL_ID)
                {
                    userData.profile = CacheClient.LoadUserProfile(userId);
                }

                // token data
                if(dataWrapper.data.ContainsKey("token"))
                {
                    userData.oAuthToken = (string)dataWrapper.data["token"];
                }
                if(dataWrapper.data.ContainsKey("wasTokenRejected"))
                {
                    userData.wasTokenRejected = (bool)dataWrapper.data["wasTokenRejected"];
                }

                // external auth data
                if(dataWrapper.data.ContainsKey("steamTicket"))
                {
                    userData.externalAuthTicket.value = (string)dataWrapper.data["steamTicket"];
                    userData.externalAuthTicket.provider = ExternalAuthenticationProvider.Steam;
                }
                if(dataWrapper.data.ContainsKey("gogTicket"))
                {
                    userData.externalAuthTicket.value = (string)dataWrapper.data["gogTicket"];
                    userData.externalAuthTicket.provider = ExternalAuthenticationProvider.GOG;
                }

                IOUtilities.DeleteFile(filePath);
            }

            // - set and save -
            UserAccountManagement.SetLocalUserData(userData);
            UserAccountManagement.SaveActiveUser();

            Debug.Log("[mod.io] UserData updated completed.");
        }
    }
}
#pragma warning restore 0618