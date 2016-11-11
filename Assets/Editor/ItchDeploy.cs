﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Compression;
using Ionic.Zip;
using System.IO;
static class ItchDeploy
{
    static string AccountName = "Avras_Chismar";
    static string ChannelName = "windows";
    public static void OnProjectBuilt(string pathToPlayer)
    {
        try
        {

            var i = pathToPlayer.LastIndexOf('/');
            pathToPlayer = pathToPlayer.Substring(0, i+ 1);

            var pathToEditor = pathToPlayer.Substring(0, pathToPlayer.LastIndexOf('/') +1);

            File.Delete(pathToEditor + "butler_creds");
            File.Delete(pathToEditor + "butler");
            File.Move(pathToPlayer + "ExperimentalProject_Data/StreamingAssets/butler_creds", pathToEditor + "butler_creds");
            File.Move(pathToPlayer + "ExperimentalProject_Data/StreamingAssets/butler", pathToEditor + "butler");
            

      
            string args = String.Format(" -i {0} push {1} {2}:{3}",
            pathToEditor + "butler_creds",
            pathToEditor + "ExperimentalProject.zip",
            AccountName,
            ChannelName                                
            );
            ZipFile file = new ZipFile();
            file.AddDirectory(pathToPlayer);
            file.Save(pathToEditor + "ExperimentalProject.zip");

            System.Diagnostics.Process uploadProc = new System.Diagnostics.Process();
            uploadProc.StartInfo.FileName = pathToEditor + "butler";
            uploadProc.StartInfo.Arguments = args;
            uploadProc.StartInfo.UseShellExecute = true;
            uploadProc.Start();

            uploadProc.WaitForExit();
        }
        catch (Exception e)
        {
           UnityEngine.Debug.Log(e);
        }
    }

        
}