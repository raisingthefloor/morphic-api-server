// Copyright 2021-2022 Raising the Floor - US, Inc.
//
// Licensed under the New BSD license. You may not use this file except in
// compliance with this License.
//
// You may obtain a copy of the License at
// https://github.com/raisingthefloor/morphic-api-server/blob/master/LICENSE.txt
//
// The R&D leading to these results received funding from the:
// * Rehabilitation Services Administration, US Dept. of Education under
//   grant H421A150006 (APCP)
// * National Institute on Disability, Independent Living, and
//   Rehabilitation Research (NIDILRR)
// * Administration for Independent Living & Dept. of Education under grants
//   H133E080022 (RERC-IT) and H133E130028/90RE5003-01-00 (UIITA-RERC)
// * European Union's Seventh Framework Programme (FP7/2007-2013) grant
//   agreement nos. 289016 (Cloud4all) and 610510 (Prosperity4All)
// * William and Flora Hewlett Foundation
// * Ontario Ministry of Research and Innovation
// * Canadian Foundation for Innovation
// * Adobe Foundation
// * Consumer Electronics Association Foundation

namespace Morphic.Server.Settings
{
     using System;
     using System.IO;

     public class MorphicAppSecret
     {
          public delegate (byte[], byte[]) GetCryptoKeyAndIVSecretsDelegate();

          public static string? GetFileMappedSecret(string group, string key)
          {
               // create a path to the secret
               // TODO: update Path.Join to use newer string-array-based single parameter when updating to a newer version of C#
               var pathToSecret = Path.Join("secrets", group, key );

               // determine if the secret exists on disk (via the runtime container volume)
               if (File.Exists(pathToSecret) == false)
               {
                    return null;
               }

               // attempt to read the contents of the secret
               try
               {
                    return System.IO.File.ReadAllText(pathToSecret);
               }
               catch
               {
                    return null;
               }
          }

          // NOTE: when supplying secrets as environment variables, we flatten them as keys; this should not be used except in controlled environments (Docker containers) or during development
          public static string? GetEnvironmentSecret(string key)
          {
               return Environment.GetEnvironmentVariable(key);
          }

          // NOTE: this function looks for secrets as file-mapped secrets first, and then looks for them in the flattened environment variable table as a backup
          public static string? GetSecret(string group, string key)
          {
               var fileMappedSecret = MorphicAppSecret.GetFileMappedSecret(group, key);
               // TODO: update "!=" to "is not" when updating to a newer version of C#
               if (fileMappedSecret != null)
               {
                    return fileMappedSecret;
               }

               var environmentSecret = MorphicAppSecret.GetEnvironmentSecret(key);
               // TODO: update "!=" to "is not" when updating to a newer version of C#
               if (environmentSecret != null)
               {
                    return environmentSecret;
               }

               // if we could not find the secret, return null
               return null;
          }
     }
}