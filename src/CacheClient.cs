﻿// #define DO_NOT_LOAD_CACHE

using System;
using System.IO;
using System.Collections.Generic;

using UnityEngine;

using Newtonsoft.Json;

using ModIO.API;

namespace ModIO
{
    /// <summary>An interface for storing/loading data retrieved for the mod.io servers on disk.</summary>
    public static class CacheClient
    {
        // ---------[ MEMBERS ]---------
        /// <summary>Directory that the CacheClient uses to store data.</summary>
        public static string cacheDirectory;

        // ---------[ INITIALIZATION ]---------
        /// <summary>Initializes the CacheClient settings.</summary>
        static CacheClient()
        {
            PluginSettings settings = PluginSettings.LoadDefaults();
            CacheClient.cacheDirectory = settings.cacheDirectory;
        }

        // ---------[ GAME PROFILE ]---------
        /// <summary>File path for the game profile data.</summary>
        public static string gameProfileFilePath
        { get { return IOUtilities.CombinePath(CacheClient.cacheDirectory, "game_profile.data"); } }

        /// <summary>Stores the game's profile in the cache.</summary>
        public static bool SaveGameProfile(GameProfile profile)
        {
            Debug.Assert(profile != null);
            return IOUtilities.WriteJsonObjectFile(gameProfileFilePath, profile);
        }

        /// <summary>Retrieves the game's profile from the cache.</summary>
        public static GameProfile LoadGameProfile()
        {
            return IOUtilities.ReadJsonObjectFile<GameProfile>(gameProfileFilePath);
        }

        // ---------[ MODS ]---------
        /// <summary>Generates the path for a mod cache directory.</summary>
        public static string GenerateModDirectoryPath(int modId)
        {
            return IOUtilities.CombinePath(CacheClient.cacheDirectory,
                                           "mods",
                                           modId.ToString());
        }

        // ------[ PROFILES ]------
        /// <summary>Generates the file path for a mod's profile data.</summary>
        public static string GenerateModProfileFilePath(int modId)
        {
            return IOUtilities.CombinePath(CacheClient.GenerateModDirectoryPath(modId),
                                           "profile.data");
        }

        /// <summary>Stores a mod's profile in the cache.</summary>
        public static bool SaveModProfile(ModProfile profile)
        {
            Debug.Assert(profile != null);
            return IOUtilities.WriteJsonObjectFile(GenerateModProfileFilePath(profile.id), profile);
        }

        /// <summary>Retrieves a mod's profile from the cache.</summary>
        public static ModProfile LoadModProfile(int modId)
        {
            string profileFilePath = GenerateModProfileFilePath(modId);
            ModProfile profile = IOUtilities.ReadJsonObjectFile<ModProfile>(profileFilePath);
            return(profile);
        }

        /// <summary>Stores a collection of mod profiles in the cache.</summary>
        public static bool SaveModProfiles(IEnumerable<ModProfile> modProfiles)
        {
            Debug.Assert(modProfiles != null);

            bool isSuccessful = true;
            foreach(ModProfile profile in modProfiles)
            {
                isSuccessful = CacheClient.SaveModProfile(profile) && isSuccessful;
            }
            return isSuccessful;
        }

        /// <summary>[Obsolete] Iterates through all of the mod profiles in the cache.</summary>
        [Obsolete("Use CacheClient.IterateAllModProfiles() instead.")]
        public static IEnumerable<ModProfile> AllModProfiles()
        { return CacheClient.IterateAllModProfiles(); }

        /// <summary>Iterates through all of the mod profiles in the cache.</summary>
        public static IEnumerable<ModProfile> IterateAllModProfiles()
        {
            return IterateAllModProfilesFromOffset(0);
        }

        /// <summary>Iterates through all of the mod profiles from the given offset.</summary>
        public static IEnumerable<ModProfile> IterateAllModProfilesFromOffset(int offset)
        {
            string profileDirectory = IOUtilities.CombinePath(CacheClient.cacheDirectory, "mods");

            if(Directory.Exists(profileDirectory))
            {
                string[] modDirectories;
                try
                {
                    modDirectories = Directory.GetDirectories(profileDirectory);
                }
                catch(Exception e)
                {
                    string warningInfo = ("[mod.io] Failed to read mod profile directory."
                                          + "\nDirectory: " + profileDirectory + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));

                    modDirectories = new string[0];
                }

                int offsetDirCount = modDirectories.Length - offset;
                if(offsetDirCount > 0)
                {
                    string[] offsetModDirectories = new string[offsetDirCount];
                    Array.Copy(modDirectories, offset,
                               offsetModDirectories, 0,
                               offsetDirCount);

                    foreach(string modDirectory in offsetModDirectories)
                    {
                        string profilePath = IOUtilities.CombinePath(modDirectory + "profile.data");
                        ModProfile profile = IOUtilities.ReadJsonObjectFile<ModProfile>(profilePath);

                        if(profile != null)
                        {
                            yield return profile;
                        }
                    }
                }
            }
        }

        /// <summary>Determines how many ModProfiles are currently stored in the cache.</summary>
        public static int CountModProfiles()
        {
            string profileDirectory = IOUtilities.CombinePath(CacheClient.cacheDirectory, "mods");

            if(Directory.Exists(profileDirectory))
            {
                string[] modDirectories;
                try
                {
                    modDirectories = Directory.GetDirectories(profileDirectory);
                }
                catch(Exception e)
                {
                    string warningInfo = ("[mod.io] Failed to read mod profile directory."
                                          + "\nDirectory: " + profileDirectory + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));

                    modDirectories = new string[0];
                }

                return modDirectories.Length;
            }

            return 0;
        }

        /// <summary>Deletes all of a mod's data from the cache.</summary>
        public static bool DeleteMod(int modId)
        {
            string modDir = CacheClient.GenerateModDirectoryPath(modId);
            return IOUtilities.DeleteDirectory(modDir);
        }

        // ------[ STATISTICS ]------
        public static string GenerateModStatisticsFilePath(int modId)
        {
            return IOUtilities.CombinePath(CacheClient.GenerateModDirectoryPath(modId),
                                           "stats.data");
        }

        public static bool SaveModStatistics(ModStatistics stats)
        {
            Debug.Assert(stats != null);
            string statsFilePath = GenerateModStatisticsFilePath(stats.modId);
            return IOUtilities.WriteJsonObjectFile(statsFilePath, stats);
        }

        public static ModStatistics LoadModStatistics(int modId)
        {
            string statsFilePath = GenerateModStatisticsFilePath(modId);
            ModStatistics stats = IOUtilities.ReadJsonObjectFile<ModStatistics>(statsFilePath);
            return(stats);
        }

        // ------[ MODFILES ]------
        /// <summary>[Obsolete] Generates the path for a cached mod build directory.</summary>
        [Obsolete("Use CacheClient.GenerateModBinariesDirectoryPath() instead.")]
        public static string GenerateModBuildsDirectoryPath(int modId)
        { return CacheClient.GenerateModBinariesDirectoryPath(modId) + "/"; }

        /// <summary>Generates the path for a cached mod build directory.</summary>
        public static string GenerateModBinariesDirectoryPath(int modId)
        {
            return IOUtilities.CombinePath(CacheClient.GenerateModDirectoryPath(modId), "binaries");
        }

        /// <summary>Generates the file path for a modfile.</summary>
        public static string GenerateModfileFilePath(int modId, int modfileId)
        {
            return IOUtilities.CombinePath(CacheClient.GenerateModBinariesDirectoryPath(modId),
                                           modfileId.ToString() + ".data");
        }

        /// <summary>Generates the file path for a mod binary.</summary>
        public static string GenerateModBinaryZipFilePath(int modId, int modfileId)
        {
            return IOUtilities.CombinePath(CacheClient.GenerateModBinariesDirectoryPath(modId),
                                           modfileId.ToString() + ".zip");
        }

        /// <summary>Stores a modfile in the cache.</summary>
        public static bool SaveModfile(Modfile modfile)
        {
            Debug.Assert(modfile != null);
            return IOUtilities.WriteJsonObjectFile(GenerateModfileFilePath(modfile.modId, modfile.id),
                                                   modfile);
        }

        /// <summary>Retrieves a modfile from the cache.</summary>
        public static Modfile LoadModfile(int modId, int modfileId)
        {
            string modfileFilePath = GenerateModfileFilePath(modId, modfileId);
            var modfile = IOUtilities.ReadJsonObjectFile<Modfile>(modfileFilePath);
            return modfile;
        }

        /// <summary>Stores a mod binary's ZipFile data in the cache.</summary>
        public static bool SaveModBinaryZip(int modId, int modfileId,
                                            byte[] modBinary)
        {
            Debug.Assert(modBinary != null);
            Debug.Assert(modBinary.Length > 0);

            string filePath = GenerateModBinaryZipFilePath(modId, modfileId);
            return IOUtilities.WriteBinaryFile(filePath, modBinary);
        }

        /// <summary>Retrieves a mod binary's ZipFile data from the cache.</summary>
        public static byte[] LoadModBinaryZip(int modId, int modfileId)
        {
            string filePath = GenerateModBinaryZipFilePath(modId, modfileId);
            byte[] zipData = IOUtilities.LoadBinaryFile(filePath);
            return zipData;
        }

        /// <summary>Deletes a modfile and binary from the cache.</summary>
        public static bool DeleteModfileAndBinaryZip(int modId, int modfileId)
        {
            bool isSuccessful = IOUtilities.DeleteFile(CacheClient.GenerateModfileFilePath(modId, modfileId));
            isSuccessful = (IOUtilities.DeleteFile(CacheClient.GenerateModBinaryZipFilePath(modId, modfileId))
                            && isSuccessful);
            return isSuccessful;
        }

        /// <summary>Deletes all modfiles and binaries from the cache.</summary>
        public static bool DeleteAllModfileAndBinaryData(int modId)
        {
            return IOUtilities.DeleteDirectory(CacheClient.GenerateModBinariesDirectoryPath(modId));
        }

        // ------[ MEDIA ]------
        /// <summary>Generates the directory path for a mod logo collection.</summary>
        public static string GenerateModLogoCollectionDirectoryPath(int modId)
        {
            return IOUtilities.CombinePath(CacheClient.GenerateModDirectoryPath(modId),
                                           "logo");
        }

        /// <summary>Generates the file path for a mod logo.</summary>
        public static string GenerateModLogoFilePath(int modId, LogoSize size)
        {
            return IOUtilities.CombinePath(GenerateModLogoCollectionDirectoryPath(modId),
                                           size.ToString() + ".png");
        }

        /// <summary>Generates the file path for a mod logo's cached version information.</summary>
        public static string GenerateModLogoVersionInfoFilePath(int modId)
        {
            return IOUtilities.CombinePath(CacheClient.GenerateModLogoCollectionDirectoryPath(modId),
                                           "versionInfo.data");
        }

        /// <summary>[Obsolete] Generates the directory path for the cached mod media.</summary>
        [Obsolete("Use CacheClient.GenerateModMediaDirectoryPath() instead.")]
        public static string GenerateModGalleryImageDirectoryPath(int modId)
        {
            return(GenerateModMediaDirectoryPath(modId));
        }

        /// <summary>Generates the directory path for the cached mod media.</summary>
        public static string GenerateModMediaDirectoryPath(int modId)
        {
            return IOUtilities.CombinePath(GenerateModDirectoryPath(modId),
                                           "mod_media");
        }

        /// <summary>Generates the file path for a mod galley image.</summary>
        public static string GenerateModGalleryImageFilePath(int modId,
                                                             string imageFileName,
                                                             ModGalleryImageSize size)
        {
            return IOUtilities.CombinePath(GenerateModMediaDirectoryPath(modId),
                                           "images_" + size.ToString(),
                                           Path.GetFileNameWithoutExtension(imageFileName) + ".png");
        }

        /// <summary>Generates the file path for a YouTube thumbnail.</summary>
        public static string GenerateModYouTubeThumbnailFilePath(int modId,
                                                                 string youTubeId)
        {
            return IOUtilities.CombinePath(GenerateModMediaDirectoryPath(modId),
                                           "youTube",
                                           youTubeId + ".png");
        }

        /// <summary>Retrieves the file paths for the mod logos in the cache.</summary>
        public static Dictionary<LogoSize, string> LoadModLogoFilePaths(int modId)
        {
            return IOUtilities.ReadJsonObjectFile<Dictionary<LogoSize, string>>(CacheClient.GenerateModLogoVersionInfoFilePath(modId));
        }

        /// <summary>Stores a mod logo in the cache with the given fileName.</summary>
        public static bool SaveModLogo(int modId, string fileName,
                                       LogoSize size, Texture2D logoTexture)
        {
            Debug.Assert(!String.IsNullOrEmpty(fileName));
            Debug.Assert(logoTexture != null);

            string logoFilePath = CacheClient.GenerateModLogoFilePath(modId, size);
            bool isSuccessful = IOUtilities.WritePNGFile(logoFilePath, logoTexture);

            // - Update the versioning info -
            var versionInfo = CacheClient.LoadModLogoFilePaths(modId);
            if(versionInfo == null)
            {
                versionInfo = new Dictionary<LogoSize, string>();
            }
            versionInfo[size] = fileName;
            isSuccessful = (IOUtilities.WriteJsonObjectFile(GenerateModLogoVersionInfoFilePath(modId), versionInfo)
                            && isSuccessful);

            return isSuccessful;
        }

        /// <summary>Retrieves a mod logo from the cache.</summary>
        public static Texture2D LoadModLogo(int modId, LogoSize size)
        {
            string logoFilePath = CacheClient.GenerateModLogoFilePath(modId, size);
            Texture2D logoTexture = IOUtilities.ReadImageFile(logoFilePath);
            return(logoTexture);
        }

        /// <summary>Retrieves a mod logo from the cache if it matches the given fileName.</summary>
        public static Texture2D LoadModLogo(int modId, string fileName, LogoSize size)
        {
            Debug.Assert(!String.IsNullOrEmpty(fileName));

            // - Ensure the logo is the correct version -
            var versionInfo = CacheClient.LoadModLogoFilePaths(modId);
            if(versionInfo != null)
            {
                string logoVersionFileName = string.Empty;
                if(versionInfo.TryGetValue(size, out logoVersionFileName)
                   && logoVersionFileName.ToUpper().Equals(fileName.ToUpper()))
                {
                    return CacheClient.LoadModLogo(modId, size);
                }
            }
            return null;
        }

        /// <summary>Stores a mod gallery image in the cache.</summary>
        public static bool SaveModGalleryImage(int modId,
                                               string imageFileName,
                                               ModGalleryImageSize size,
                                               Texture2D imageTexture)
        {
            Debug.Assert(!String.IsNullOrEmpty(imageFileName));
            Debug.Assert(imageTexture != null);

            string imageFilePath = CacheClient.GenerateModGalleryImageFilePath(modId,
                                                                               imageFileName,
                                                                               size);
            return IOUtilities.WritePNGFile(imageFilePath, imageTexture);
        }

        /// <summary>Retrieves a mod gallery image from the cache.</summary>
        public static Texture2D LoadModGalleryImage(int modId,
                                                    string imageFileName,
                                                    ModGalleryImageSize size)
        {
            Debug.Assert(!String.IsNullOrEmpty(imageFileName));

            string imageFilePath = CacheClient.GenerateModGalleryImageFilePath(modId,
                                                                               imageFileName,
                                                                               size);
            Texture2D imageTexture = IOUtilities.ReadImageFile(imageFilePath);

            return(imageTexture);
        }

        /// <summary>Stores a YouTube thumbnail in the cache.</summary>
        public static bool SaveModYouTubeThumbnail(int modId,
                                                   string youTubeId,
                                                   Texture2D thumbnail)
        {
            Debug.Assert(!String.IsNullOrEmpty(youTubeId));
            Debug.Assert(thumbnail != null);

            string thumbnailFilePath = CacheClient.GenerateModYouTubeThumbnailFilePath(modId,
                                                                                       youTubeId);
            return IOUtilities.WritePNGFile(thumbnailFilePath, thumbnail);
        }

        /// <summary>Retrieves a YouTube thumbnail from the cache.</summary>
        public static Texture2D LoadModYouTubeThumbnail(int modId,
                                                        string youTubeId)
        {
            Debug.Assert(!String.IsNullOrEmpty(youTubeId));

            string thumbnailFilePath = CacheClient.GenerateModYouTubeThumbnailFilePath(modId,
                                                                                       youTubeId);

            Texture2D thumbnailTexture = IOUtilities.ReadImageFile(thumbnailFilePath);

            return(thumbnailTexture);
        }

        // ---------[ MOD TEAMS ]---------
        /// <summary>Generates the file path for a mod team's data.</summary>
        public static string GenerateModTeamFilePath(int modId)
        {
            return IOUtilities.CombinePath(CacheClient.GenerateModDirectoryPath(modId),
                                           "team.data");
        }

        /// <summary>Stores a mod team's data in the cache.</summary>
        public static bool SaveModTeam(int modId,
                                       List<ModTeamMember> modTeam)
        {
            Debug.Assert(modTeam != null);

            string filePath = CacheClient.GenerateModTeamFilePath(modId);
            return IOUtilities.WriteJsonObjectFile(filePath, modTeam);
        }

        /// <summary>Retrieves a mod team's data from the cache.</summary>
        public static List<ModTeamMember> LoadModTeam(int modId)
        {
            string filePath = CacheClient.GenerateModTeamFilePath(modId);
            var modTeam = IOUtilities.ReadJsonObjectFile<List<ModTeamMember>>(filePath);
            return modTeam;
        }

        /// <summary>Deletes a mod team's data from the cache.</summary>
        public static bool DeleteModTeam(int modId)
        {
            return IOUtilities.DeleteFile(CacheClient.GenerateModTeamFilePath(modId));
        }

        // ---------[ USERS ]---------
        /// <summary>Generates the file path for a user's profile.</summary>
        public static string GenerateUserProfileFilePath(int userId)
        {
            return IOUtilities.CombinePath(CacheClient.cacheDirectory,
                                           "users",
                                           userId.ToString(),
                                           "profile.data");
        }

        /// <summary>Generates the file path for a user's profile.</summary>
        public static string GenerateUserAvatarDirectoryPath(int userId)
        {
            return IOUtilities.CombinePath(CacheClient.cacheDirectory,
                                           "users",
                                           userId + "_avatar");
        }

        /// <summary>Generates the file path for a user's profile.</summary>
        public static string GenerateUserAvatarFilePath(int userId, UserAvatarSize size)
        {
            return IOUtilities.CombinePath(CacheClient.GenerateUserAvatarDirectoryPath(userId),
                                           size.ToString() + ".png");
        }

        /// <summary>Stores a user's profile in the cache.</summary>
        public static bool SaveUserProfile(UserProfile userProfile)
        {
            Debug.Assert(userProfile != null);

            string filePath = CacheClient.GenerateUserProfileFilePath(userProfile.id);
            return IOUtilities.WriteJsonObjectFile(filePath, userProfile);
        }

        /// <summary>Retrieves a user's profile from the cache.</summary>
        public static UserProfile LoadUserProfile(int userId)
        {
            string filePath = CacheClient.GenerateUserProfileFilePath(userId);
            var userProfile = IOUtilities.ReadJsonObjectFile<UserProfile>(filePath);
            return(userProfile);
        }

        /// <summary>Deletes a user's profile from the cache.</summary>
        public static bool DeleteUserProfile(int userId)
        {
            return IOUtilities.DeleteFile(CacheClient.GenerateUserProfileFilePath(userId));
        }

        /// <summary>Iterates through all the user profiles in the cache.</summary>
        public static IEnumerable<UserProfile> IterateAllUserProfiles()
        {
            string profileDirectory = IOUtilities.CombinePath(CacheClient.cacheDirectory,
                                                              "users");

            if(Directory.Exists(profileDirectory))
            {
                string[] userFiles;
                try
                {
                    userFiles = Directory.GetFiles(profileDirectory);
                }
                catch(Exception e)
                {
                    string warningInfo = ("[mod.io] Failed to read user profile directory."
                                          + "\nDirectory: " + profileDirectory + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));

                    userFiles = new string[0];
                }

                foreach(string profileFilePath in userFiles)
                {
                    var profile = IOUtilities.ReadJsonObjectFile<UserProfile>(profileFilePath);
                    if(profile != null)
                    {
                        yield return profile;
                    }
                }
            }
        }

        /// <summary>Stores a user's avatar in the cache.</summary>
        public static bool SaveUserAvatar(int userId, UserAvatarSize size,
                                          Texture2D avatarTexture)
        {
            Debug.Assert(avatarTexture != null);

            string avatarFilePath = CacheClient.GenerateUserAvatarFilePath(userId, size);
            return IOUtilities.WritePNGFile(avatarFilePath, avatarTexture);
        }

        /// <summary>Retrieves a user's avatar from the cache.</summary>
        public static Texture2D LoadUserAvatar(int userId, UserAvatarSize size)
        {
            string avatarFilePath = CacheClient.GenerateUserAvatarFilePath(userId, size);
            Texture2D avatarTexture = IOUtilities.ReadImageFile(avatarFilePath);
            return(avatarTexture);
        }

        /// <summary>Delete's a user's avatars from the cache.</summary>
        public static bool DeleteUserAvatar(int userId)
        {
            return IOUtilities.DeleteDirectory(CacheClient.GenerateUserAvatarDirectoryPath(userId));
        }


        // ---------[ BASIC FILE I/O ]---------
        /// <summary>Reads an entire file and parses the JSON Object it contains.</summary>
        [Obsolete("Use IOUtilities.ReadJsonObjectFile() instead.")]
        public static T ReadJsonObjectFile<T>(string filePath)
        {
            return IOUtilities.ReadJsonObjectFile<T>(filePath);
        }

        /// <summary>Writes an object to a file in the JSON Object format.</summary>
        [Obsolete("Use IOUtilities.WriteJsonObjectFile() instead.")]
        public static bool WriteJsonObjectFile<T>(string filePath,
                                                  T jsonObject)
        {
            return IOUtilities.WriteJsonObjectFile(filePath, jsonObject);
        }

        /// <summary>Loads an entire binary file as a byte array.</summary>
        [Obsolete("Use IOUtilities.LoadBinaryFile() instead.")]
        public static byte[] LoadBinaryFile(string filePath)
        {
            return IOUtilities.LoadBinaryFile(filePath);
        }

        /// <summary>Writes an entire binary file.</summary>
        [Obsolete("Use IOUtilities.WriteBinaryFile() instead.")]
        public static bool WriteBinaryFile(string filePath,
                                           byte[] data)
        {
            return IOUtilities.WriteBinaryFile(filePath, data);
        }

        /// <summary>Loads the image data from a file into a new Texture.</summary>
        [Obsolete("Use IOUtilities.ReadImageFile() instead.")]
        public static Texture2D ReadImageFile(string filePath)
        {
            return IOUtilities.ReadImageFile(filePath);
        }

        /// <summary>Writes a texture to a PNG file.</summary>
        [Obsolete("Use IOUtilities.WritePNGFile() instead.")]
        public static bool WritePNGFile(string filePath,
                                        Texture2D texture)
        {
            return IOUtilities.WritePNGFile(filePath, texture);
        }

        /// <summary>Deletes a file.</summary>
        [Obsolete("Use IOUtilities.DeleteFile() instead.")]
        public static bool DeleteFile(string filePath)
        {
            return IOUtilities.DeleteFile(filePath);
        }

        /// <summary>Deletes a directory.</summary>
        [Obsolete("Use IOUtilities.DeleteDirectory() instead.")]
        public static bool DeleteDirectory(string directoryPath)
        {
            return IOUtilities.DeleteDirectory(directoryPath);
        }

        /// <summary>[Obsolete] Retrieves the directory the CacheClient uses.</summary>
        [Obsolete("Use CacheClient.cacheDirectory instead.")]
        public static string GetCacheDirectory()
        {
            return CacheClient.cacheDirectory;
        }
    }
}
