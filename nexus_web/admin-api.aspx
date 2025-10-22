<%@ Page Language="C#" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Web.Script.Serialization" %>
<%@ Import Namespace="System.Diagnostics" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="System.Net" %>
<%
    string action = Request.QueryString["action"] ?? "";
    
    try 
    {
        switch (action.ToLower())
        {
            case "status":
                HandleStatusRequest();
                break;
            case "debug":
                HandleDebugRequest();
                break;
            case "restart":
                HandleRestartRequest();
                break;
            case "processes":
                HandleProcessesRequest();
                break;
            case "kill":
                HandleKillRequest();
                break;
            case "filesystem":
                HandleFileSystemRequest();
                break;
            case "cleanup":
                HandleCleanupRequest();
                break;
            case "health":
                HandleHealthRequest();
                break;
            case "wsl":
                HandleWSLRequest();
                break;
            case "resetlist":
                HandleResetListRequest();
                break;
            case "refreshlist":
                HandleRefreshListRequest();
                break;
            case "deletefailed":
                HandleDeleteFailedRequest();
                break;
            case "deletejob":
                HandleDeleteJobRequest();
                break;
            default:
                Response.ContentType = "application/json";
                Response.Write("{\"success\": false, \"error\": \"Unknown action: " + action + "\"}");
                break;
        }
    }
    catch (Exception ex)
    {
        Response.ContentType = "application/json";
        Response.Write("{\"success\": false, \"error\": \"" + ex.Message.Replace("\"", "\\\"") + "\"}");
    }
%>

<script runat="server">
    void HandleStatusRequest()
    {
        Response.ContentType = "application/json";
        
        var stats = new Dictionary<string, object>();
        
        // Queue statistics
        string queueFile = Server.MapPath("~/App_Data/queue.json");
        int queueCount = 0, processingCount = 0, completedCount = 0, errorCount = 0;
        
        if (File.Exists(queueFile))
        {
            try
            {
                string json = File.ReadAllText(queueFile);
                var serializer = new JavaScriptSerializer();
                var queueData = serializer.DeserializeObject(json) as Dictionary<string, object>;
                
                if (queueData != null && queueData.ContainsKey("jobs"))
                {
                    var jobs = queueData["jobs"] as object[];
                    if (jobs != null)
                    {
                        queueCount = jobs.Length;
                        foreach (var jobObj in jobs)
                        {
                            var job = jobObj as Dictionary<string, object>;
                            if (job != null && job.ContainsKey("status"))
                            {
                                string status = job["status"].ToString().ToLower();
                                switch (status)
                                {
                                    case "processing": processingCount++; break;
                                    case "completed": completedCount++; break;
                                    case "error": errorCount++; break;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }
        
        // WSL cursor-agent process count
        int processCount = 0;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = "-u droller bash -l -c \"ps aux | grep -E 'cursor-agent|node.*cursor' | grep -v grep | wc -l\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using (var process = Process.Start(psi))
            {
                if (process != null)
                {
                    process.WaitForExit(5000);
                    if (process.HasExited)
                    {
                        string output = process.StandardOutput.ReadToEnd().Trim();
                        int.TryParse(output, out processCount);
                    }
                }
            }
        }
        catch { }
        
        // Upload files count
        int uploadCount = 0;
        try
        {
            string uploadsDir = Server.MapPath("~/uploads");
            if (Directory.Exists(uploadsDir))
            {
                uploadCount = Directory.GetFiles(uploadsDir).Length;
            }
        }
        catch { }
        
        stats["queueCount"] = queueCount;
        stats["processingCount"] = processingCount;
        stats["completedCount"] = completedCount;
        stats["errorCount"] = errorCount;
        stats["processCount"] = processCount;
        stats["uploadCount"] = uploadCount;
        
        var result = new { success = true, stats = stats };
        var serializer2 = new JavaScriptSerializer();
        Response.Write(serializer2.Serialize(result));
    }

    void HandleDebugRequest()
    {
        Response.ContentType = "text/plain";
        Response.Write("=== SYSTEM DEBUG INFORMATION ===\n\n");
        
        // Queue file info
        string queueFile = Server.MapPath("~/App_Data/queue.json");
        Response.Write("Queue File: " + queueFile + "\n");
        Response.Write("Queue File Exists: " + File.Exists(queueFile) + "\n");
        
        if (File.Exists(queueFile))
        {
            try
            {
                string json = File.ReadAllText(queueFile);
                Response.Write("Queue File Size: " + json.Length + " bytes\n");
                Response.Write("Queue Content Preview:\n" + json.Substring(0, Math.Min(500, json.Length)) + "\n\n");
            }
            catch (Exception ex)
            {
                Response.Write("Error reading queue file: " + ex.Message + "\n\n");
            }
        }
        
        // Directory info
        Response.Write("=== DIRECTORIES ===\n");
        string[] dirs = { "~/uploads", "~/analysis", "~/App_Data", "~/logs" };
        foreach (string dir in dirs)
        {
            try
            {
                string fullPath = Server.MapPath(dir);
                Response.Write(dir + ": " + fullPath + "\n");
                Response.Write("  Exists: " + Directory.Exists(fullPath) + "\n");
                if (Directory.Exists(fullPath))
                {
                    var files = Directory.GetFiles(fullPath);
                    var subdirs = Directory.GetDirectories(fullPath);
                    Response.Write("  Files: " + files.Length + ", Subdirs: " + subdirs.Length + "\n");
                }
            }
            catch (Exception ex)
            {
                Response.Write("  Error: " + ex.Message + "\n");
            }
        }
        
        Response.Write("\n=== WSL CURSOR-AGENT PROCESSES ===\n");
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = "-u droller bash -l -c \"ps aux | grep -E 'cursor-agent|node.*cursor' | grep -v grep || echo 'No cursor-agent processes found'\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using (var process = Process.Start(psi))
            {
                if (process != null)
                {
                    process.WaitForExit(5000);
                    if (process.HasExited)
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        var lines = output.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
                        
                        if (lines.Length > 0 && !output.Contains("No cursor-agent processes found"))
                        {
                            Response.Write("Found cursor-agent processes in WSL:\n");
                            foreach (var line in lines)
                            {
                                Response.Write("  " + line.Trim() + "\n");
                            }
                        }
                        else
                        {
                            Response.Write("No cursor-agent processes currently running in WSL\n");
                        }
                    }
                    else
                    {
                        Response.Write("Process check timed out\n");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Response.Write("Error getting WSL processes: " + ex.Message + "\n");
        }
        
        Response.Write("\n=== SYSTEM INFO ===\n");
        Response.Write("Server Time: " + DateTime.Now + "\n");
        Response.Write("Machine Name: " + Environment.MachineName + "\n");
        Response.Write("OS Version: " + Environment.OSVersion + "\n");
        Response.Write("Working Directory: " + Environment.CurrentDirectory + "\n");
        Response.Write("Temp Path: " + Path.GetTempPath() + "\n");
    }

    void HandleRestartRequest()
    {
        Response.ContentType = "text/plain";
        Response.Write("=== RESTARTING QUEUE PROCESSING ===\n\n");
        
        // Kill hanging cursor-agent processes in WSL only
        Response.Write("Killing hanging cursor-agent processes in WSL...\n");
        try
        {
            var killPsi = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = "-u droller bash -l -c \"pkill -9 -f 'cursor-agent' || true; pkill -9 -f 'node.*cursor-agent' || true; echo 'WSL cursor-agent processes killed'\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using (var p = Process.Start(killPsi))
            {
                if (p != null)
                {
                    p.WaitForExit(10000);
                    if (!p.HasExited) p.Kill();
                    
                    string output = p.StandardOutput.ReadToEnd();
                    Response.Write("WSL cleanup result: " + output + "\n");
                }
            }
        }
        catch (Exception ex)
        {
            Response.Write("Error killing WSL processes: " + ex.Message + "\n");
        }
        
        // Reset stuck jobs
        string queueFile = Server.MapPath("~/App_Data/queue.json");
        if (File.Exists(queueFile))
        {
            try
            {
                string json = File.ReadAllText(queueFile);
                var serializer = new JavaScriptSerializer();
                var queueData = serializer.DeserializeObject(json) as Dictionary<string, object>;
                
                if (queueData != null && queueData.ContainsKey("jobs"))
                {
                    var jobs = queueData["jobs"] as object[];
                    if (jobs != null)
                    {
                        var resetCount = 0;
                        foreach (var jobObj in jobs)
                        {
                            var jobDict = jobObj as Dictionary<string, object>;
                            if (jobDict != null && jobDict.ContainsKey("status") && jobDict["status"].ToString() == "processing")
                            {
                                DateTime updated;
                                if (DateTime.TryParse(jobDict["updated"].ToString(), out updated))
                                {
                                    if (DateTime.Now - updated > TimeSpan.FromMinutes(15))
                                    {
                                        Response.Write("Resetting stuck job: " + jobDict["name"] + "\n");
                                        jobDict["status"] = "pending";
                                        jobDict["updated"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                        resetCount++;
                                    }
                                }
                            }
                        }
                        
                        if (resetCount > 0)
                        {
                            var updatedQueueData = new { jobs = jobs };
                            File.WriteAllText(queueFile, serializer.Serialize(updatedQueueData));
                            Response.Write("Reset " + resetCount + " stuck jobs to pending\n");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Response.Write("Error resetting jobs: " + ex.Message + "\n");
            }
        }
        
        // Trigger processor
        try
        {
            Response.Write("\nTriggering queue processor...\n");
            var request = WebRequest.Create(Request.Url.Scheme + "://" + Request.Url.Authority + "/process.aspx");
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Timeout = 5000;
            
            using (var response = request.GetResponse())
            {
                Response.Write("Queue processor triggered successfully\n");
            }
        }
        catch (Exception ex)
        {
            Response.Write("Note: Could not trigger processor automatically: " + ex.Message + "\n");
        }
        
        Response.Write("\nRestart complete!\n");
    }

    void HandleProcessesRequest()
    {
        Response.ContentType = "text/plain";
        Response.Write("=== WSL CURSOR-AGENT PROCESSES ===\n\n");
        
        try
        {
            // Check for cursor-agent processes in WSL
            var psi = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = "-u droller bash -l -c \"ps aux | grep -E 'cursor-agent|node.*cursor' | grep -v grep || echo 'No cursor-agent processes found'\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using (var process = Process.Start(psi))
            {
                if (process != null)
                {
                    process.WaitForExit(10000);
                    if (!process.HasExited)
                    {
                        process.Kill();
                        Response.Write("[ERROR] Process check timed out\n");
                        return;
                    }
                    
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    
                    Response.Write("WSL Process List:\n");
                    Response.Write("================\n");
                    
                    if (!string.IsNullOrEmpty(output))
                    {
                        var lines = output.Split('\n');
                        var processCount = 0;
                        
                        foreach (var line in lines)
                        {
                            var trimmedLine = line.Trim();
                            if (!string.IsNullOrEmpty(trimmedLine) && !trimmedLine.Contains("No cursor-agent processes found"))
                            {
                                Response.Write(trimmedLine + "\n");
                                processCount++;
                            }
                        }
                        
                        if (processCount == 0)
                        {
                            Response.Write("[INFO] No cursor-agent processes currently running in WSL\n");
                        }
                        else
                        {
                            Response.Write("\n[INFO] Found " + processCount + " cursor-agent related processes\n");
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(error))
                    {
                        Response.Write("\nErrors:\n" + error + "\n");
                    }
                }
            }
            
            // Also show WSL system info
            Response.Write("\n=== WSL SYSTEM INFO ===\n");
            var sysInfoPsi = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = "-u droller bash -l -c \"echo 'User: $(whoami)'; echo 'PWD: $(pwd)'; echo 'Load: $(uptime | cut -d',' -f3-5)'; echo 'Memory:'; free -h | head -2\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using (var sysProcess = Process.Start(sysInfoPsi))
            {
                if (sysProcess != null)
                {
                    sysProcess.WaitForExit(5000);
                    if (sysProcess.HasExited)
                    {
                        string sysOutput = sysProcess.StandardOutput.ReadToEnd();
                        Response.Write(sysOutput + "\n");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Response.Write("[ERROR] Error checking WSL processes: " + ex.Message + "\n");
        }
    }

    void HandleKillRequest()
    {
        Response.ContentType = "text/plain";
        Response.Write("=== KILLING WSL CURSOR-AGENT PROCESSES ===\n\n");
        
        try
        {
            // First, list current processes to show what we're about to kill
            Response.Write("Checking for cursor-agent processes in WSL...\n");
            var checkPsi = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = "-u droller bash -l -c \"ps aux | grep -E 'cursor-agent|node.*cursor' | grep -v grep || echo 'No cursor-agent processes found'\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using (var checkProcess = Process.Start(checkPsi))
            {
                if (checkProcess != null)
                {
                    checkProcess.WaitForExit(5000);
                    if (checkProcess.HasExited)
                    {
                        string checkOutput = checkProcess.StandardOutput.ReadToEnd();
                        var lines = checkOutput.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l) && !l.Contains("No cursor-agent processes found")).ToArray();
                        
                        if (lines.Length > 0)
                        {
                            Response.Write("Found processes to kill:\n");
                            foreach (var line in lines)
                            {
                                Response.Write("  " + line.Trim() + "\n");
                            }
                            Response.Write("\n");
                        }
                        else
                        {
                            Response.Write("[INFO] No cursor-agent processes found in WSL\n");
                            return;
                        }
                    }
                }
            }
            
            // Kill all cursor-agent processes
            Response.Write("Executing kill commands...\n");
            var killPsi = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = "-u droller bash -l -c \"pkill -9 -f 'cursor-agent' 2>/dev/null; pkill -9 -f 'node.*cursor-agent' 2>/dev/null; echo 'Kill commands executed'\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using (var killProcess = Process.Start(killPsi))
            {
                if (killProcess != null)
                {
                    killProcess.WaitForExit(10000);
                    if (!killProcess.HasExited)
                    {
                        killProcess.Kill();
                        Response.Write("[ERROR] Kill operation timed out\n");
                        return;
                    }
                    
                    string killOutput = killProcess.StandardOutput.ReadToEnd();
                    string killError = killProcess.StandardError.ReadToEnd();
                    
                    Response.Write("Kill operation result:\n");
                    if (!string.IsNullOrEmpty(killOutput))
                    {
                        Response.Write(killOutput + "\n");
                    }
                    if (!string.IsNullOrEmpty(killError))
                    {
                        Response.Write("Errors: " + killError + "\n");
                    }
                }
            }
            
            // Verify processes were killed
            Response.Write("\nVerifying processes were terminated...\n");
            System.Threading.Thread.Sleep(2000); // Wait 2 seconds
            
            var verifyPsi = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = "-u droller bash -l -c \"ps aux | grep -E 'cursor-agent|node.*cursor' | grep -v grep || echo 'No cursor-agent processes remaining'\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using (var verifyProcess = Process.Start(verifyPsi))
            {
                if (verifyProcess != null)
                {
                    verifyProcess.WaitForExit(5000);
                    if (verifyProcess.HasExited)
                    {
                        string verifyOutput = verifyProcess.StandardOutput.ReadToEnd();
                        var remainingLines = verifyOutput.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l) && !l.Contains("No cursor-agent processes remaining")).ToArray();
                        
                        if (remainingLines.Length == 0)
                        {
                            Response.Write("[OK] All cursor-agent processes successfully terminated\n");
                        }
                        else
                        {
                            Response.Write("[WARNING] Some processes may still be running:\n");
                            foreach (var line in remainingLines)
                            {
                                Response.Write("  " + line.Trim() + "\n");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Response.Write("[ERROR] Error killing WSL processes: " + ex.Message + "\n");
        }
        
        Response.Write("\n=== KILL OPERATION COMPLETE ===\n");
        Response.Write("[INFO] Targeted only cursor-agent processes in WSL environment\n");
        Response.Write("[INFO] Windows cursor IDE processes were not affected\n");
    }

    void HandleFileSystemRequest()
    {
        Response.ContentType = "text/plain";
        Response.Write("=== FILE SYSTEM INFORMATION ===\n\n");
        
        string[] directories = { "~/uploads", "~/analysis", "~/App_Data", "~/logs", "~/workingdir" };
        
        foreach (string dir in directories)
        {
            try
            {
                string fullPath = Server.MapPath(dir);
                Response.Write("Directory: " + dir + "\n");
                Response.Write("Full Path: " + fullPath + "\n");
                Response.Write("Exists: " + Directory.Exists(fullPath) + "\n");
                
                if (Directory.Exists(fullPath))
                {
                    var files = Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories);
                    var dirs = Directory.GetDirectories(fullPath, "*", SearchOption.AllDirectories);
                    
                    long totalSize = 0;
                    foreach (string file in files)
                    {
                        try
                        {
                            totalSize += new FileInfo(file).Length;
                        }
                        catch { }
                    }
                    
                    Response.Write("Files: " + files.Length + "\n");
                    Response.Write("Subdirectories: " + dirs.Length + "\n");
                    Response.Write("Total Size: " + FormatFileSize(totalSize) + "\n");
                    
                    if (dir == "~/uploads" && files.Length > 0)
                    {
                        Response.Write("Recent uploads:\n");
                        var recentFiles = files.OrderByDescending(f => new FileInfo(f).LastWriteTime).Take(5);
                        foreach (string file in recentFiles)
                        {
                            var info = new FileInfo(file);
                            Response.Write("  " + info.Name + " (" + FormatFileSize(info.Length) + ", " + info.LastWriteTime + ")\n");
                        }
                    }
                }
                
                Response.Write("\n");
            }
            catch (Exception ex)
            {
                Response.Write("Error accessing " + dir + ": " + ex.Message + "\n\n");
            }
        }
    }

    void HandleCleanupRequest()
    {
        Response.ContentType = "text/plain";
        Response.Write("=== CLEANING UP TEMPORARY FILES ===\n\n");
        
        var totalDeletedCount = 0;
        var totalDeletedSize = 0L;
        
        // Clean uploads directory
        Response.Write("Cleaning uploads directory...\n");
        string uploadsDir = Server.MapPath("~/uploads");
        if (!Directory.Exists(uploadsDir))
        {
            Response.Write("[INFO] Uploads directory does not exist\n");
        }
        else
        {
            var files = Directory.GetFiles(uploadsDir);
            var deletedCount = 0;
            var deletedSize = 0L;
            
            foreach (string file in files)
            {
                try
                {
                    var info = new FileInfo(file);
                    // Delete files older than 1 hour
                    if (DateTime.Now - info.LastWriteTime > TimeSpan.FromHours(1))
                    {
                        Response.Write("[OK] Deleting: " + info.Name + " (" + FormatFileSize(info.Length) + ")\n");
                        deletedSize += info.Length;
                        File.Delete(file);
                        deletedCount++;
                    }
                    else
                    {
                        Response.Write("[SKIP] Keeping: " + info.Name + " (recent file)\n");
                    }
                }
                catch (Exception ex)
                {
                    Response.Write("[ERROR] Error deleting " + Path.GetFileName(file) + ": " + ex.Message + "\n");
                }
            }
            
            Response.Write("[OK] Uploads cleanup: " + deletedCount + " files (" + FormatFileSize(deletedSize) + ")\n");
            totalDeletedCount += deletedCount;
            totalDeletedSize += deletedSize;
        }
        
        // Clean working directories
        Response.Write("\nCleaning working directories...\n");
        string workingDir = Server.MapPath("~/workingdir");
        if (!Directory.Exists(workingDir))
        {
            Response.Write("[INFO] Working directory does not exist\n");
        }
        else
        {
            var workDirs = Directory.GetDirectories(workingDir);
            var deletedDirCount = 0;
            
            foreach (string dir in workDirs)
            {
                try
                {
                    var dirInfo = new DirectoryInfo(dir);
                    // Delete directories older than 2 hours
                    if (DateTime.Now - dirInfo.LastWriteTime > TimeSpan.FromHours(2))
                    {
                        Response.Write("[OK] Deleting directory: " + dirInfo.Name + "\n");
                        Directory.Delete(dir, true);
                        deletedDirCount++;
                    }
                    else
                    {
                        Response.Write("[SKIP] Keeping: " + dirInfo.Name + " (recent directory)\n");
                    }
                }
                catch (Exception ex)
                {
                    Response.Write("[ERROR] Error deleting directory " + Path.GetFileName(dir) + ": " + ex.Message + "\n");
                }
            }
            
            Response.Write("[OK] Working directories cleanup: " + deletedDirCount + " directories\n");
        }
        
        // Clean old work_ directories from analysis (backward compatibility)
        Response.Write("\nCleaning old work directories from analysis...\n");
        string analysisDir = Server.MapPath("~/analysis");
        if (!Directory.Exists(analysisDir))
        {
            Response.Write("[INFO] analysis directory does not exist\n");
        }
        else
        {
            var oldWorkDirs = Directory.GetDirectories(analysisDir).Where(d => Path.GetFileName(d).StartsWith("work_")).ToArray();
            var deletedOldDirCount = 0;
            
            foreach (string dir in oldWorkDirs)
            {
                try
                {
                    var dirInfo = new DirectoryInfo(dir);
                    Response.Write("[OK] Deleting old work directory: " + dirInfo.Name + "\n");
                    Directory.Delete(dir, true);
                    deletedOldDirCount++;
                }
                catch (Exception ex)
                {
                    Response.Write("[ERROR] Error deleting old work directory " + Path.GetFileName(dir) + ": " + ex.Message + "\n");
                }
            }
            
            Response.Write("[OK] Old work directories cleanup: " + deletedOldDirCount + " directories\n");
        }
        
        Response.Write("\n=== CLEANUP COMPLETE ===\n");
        Response.Write("[OK] Total files deleted: " + totalDeletedCount + " (" + FormatFileSize(totalDeletedSize) + ")\n");
        Response.Write("[INFO] All temporary files and directories cleaned up\n");
    }

    void HandleHealthRequest()
    {
        Response.ContentType = "text/plain";
        Response.Write("=== SYSTEM HEALTH CHECK ===\n\n");
        
        // Check directories
        Response.Write("Directory Health:\n");
        string[] requiredDirs = { "~/uploads", "~/analysis", "~/App_Data", "~/workingdir" };
        foreach (string dir in requiredDirs)
        {
            string fullPath = Server.MapPath(dir);
            bool exists = Directory.Exists(fullPath);
            Response.Write("  " + dir + ": " + (exists ? "[OK]" : "[MISSING]") + "\n");
            
            if (!exists)
            {
                try
                {
                    Directory.CreateDirectory(fullPath);
                    Response.Write("    Created directory\n");
                }
                catch (Exception ex)
                {
                    Response.Write("    Failed to create: " + ex.Message + "\n");
                }
            }
        }
        
        // Check queue file
        Response.Write("\nQueue Health:\n");
        string queueFile = Server.MapPath("~/App_Data/queue.json");
        if (File.Exists(queueFile))
        {
            try
            {
                string json = File.ReadAllText(queueFile);
                var serializer = new JavaScriptSerializer();
                var queueData = serializer.DeserializeObject(json);
                Response.Write("  Queue file: ✓ OK (valid JSON)\n");
            }
            catch
            {
                Response.Write("  Queue file: ✗ CORRUPTED (invalid JSON)\n");
            }
        }
        else
        {
            Response.Write("  Queue file: ⚠ MISSING (will be created on first upload)\n");
        }
        
        // Check disk space
        Response.Write("\nDisk Space:\n");
        try
        {
            string rootPath = Server.MapPath("~/");
            var drive = new DriveInfo(Path.GetPathRoot(rootPath));
            var freeGB = drive.AvailableFreeSpace / 1024 / 1024 / 1024;
            var totalGB = drive.TotalSize / 1024 / 1024 / 1024;
            var usedPercent = (double)(drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize * 100;
            
            Response.Write("  Free space: " + freeGB + " GB (" + (100 - usedPercent).ToString("F1") + "% free)\n");
            Response.Write("  Total space: " + totalGB + " GB\n");
            
            if (freeGB < 1)
            {
                Response.Write("  Status: ✗ LOW DISK SPACE\n");
            }
            else if (freeGB < 5)
            {
                Response.Write("  Status: ⚠ DISK SPACE WARNING\n");
            }
            else
            {
                Response.Write("  Status: ✓ OK\n");
            }
        }
        catch (Exception ex)
        {
            Response.Write("  Error checking disk space: " + ex.Message + "\n");
        }
        
        Response.Write("\nOverall Status: System appears to be functioning normally\n");
    }

    void HandleWSLRequest()
    {
        Response.ContentType = "text/plain";
        Response.Write("=== WSL CONNECTIVITY TEST ===\n\n");
        
        try
        {
            Response.Write("Testing WSL basic connectivity...\n");
            
            var startInfo = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = "echo 'WSL Test: Hello from Linux'",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit(10000); // 10 second timeout
                
                if (!process.HasExited)
                {
                    process.Kill();
                    Response.Write("✗ WSL test timed out\n");
                    return;
                }
                
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                
                Response.Write("Exit Code: " + process.ExitCode + "\n");
                Response.Write("Output: " + output + "\n");
                
                if (!string.IsNullOrEmpty(error))
                {
                    Response.Write("Error: " + error + "\n");
                }
                
                if (process.ExitCode == 0 && output.Contains("Hello from Linux"))
                {
                    Response.Write("✓ WSL connectivity: OK\n");
                }
                else
                {
                    Response.Write("✗ WSL connectivity: FAILED\n");
                }
            }
        }
        catch (Exception ex)
        {
            Response.Write("✗ WSL test failed: " + ex.Message + "\n");
        }
        
        // Test cursor-agent availability
        Response.Write("\nTesting cursor-agent availability...\n");
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = "which cursor-agent",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit(5000);
                
                if (!process.HasExited)
                {
                    process.Kill();
                    Response.Write("✗ cursor-agent check timed out\n");
                }
                else
                {
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    
                    if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        Response.Write("✓ cursor-agent found at: " + output + "\n");
                    }
                    else
                    {
                        Response.Write("✗ cursor-agent not found in PATH\n");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Response.Write("✗ cursor-agent check failed: " + ex.Message + "\n");
        }
    }

    void HandleResetListRequest()
    {
        Response.ContentType = "text/plain";
        Response.Write("=== RESETTING ANALYSIS LIST ===\n\n");
        
        try
        {
            // Step 1: Clear the server-side queue
            Response.Write("Step 1: Clearing server-side queue...\n");
            string queueFile = Server.MapPath("~/App_Data/queue.json");
            if (File.Exists(queueFile))
            {
                try
                {
                    // Create empty queue
                    var emptyQueue = new { jobs = new object[0] };
                    var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                    File.WriteAllText(queueFile, serializer.Serialize(emptyQueue));
                    Response.Write("[OK] Queue cleared successfully\n");
                }
                catch (Exception ex)
                {
                    Response.Write("[ERROR] Error clearing queue: " + ex.Message + "\n");
                }
            }
            else
            {
                Response.Write("[INFO] Queue file does not exist (already empty)\n");
            }
            
            // Step 2: Hide analysis folders
            Response.Write("\nStep 2: Hiding analysis folders...\n");
            string analysisDir = Server.MapPath("~/analysis");
            if (!Directory.Exists(analysisDir))
            {
                Response.Write("[INFO] analysis directory does not exist.\n");
            }
            else
            {
                var directories = Directory.GetDirectories(analysisDir);
                var movedCount = 0;
                
                if (directories.Length == 0)
                {
                    Response.Write("[INFO] No analysis folders to hide\n");
                }
                else
                {
                    // Create a hidden directory to move folders to
                    string hiddenDir = Path.Combine(analysisDir, "_hidden");
                    if (!Directory.Exists(hiddenDir))
                    {
                        Directory.CreateDirectory(hiddenDir);
                        Response.Write("[OK] Created hidden directory: " + hiddenDir + "\n");
                    }
                    
                    foreach (string dir in directories)
                    {
                        string dirName = Path.GetFileName(dir);
                        if (dirName.StartsWith("_")) continue; // Skip already hidden directories
                        
                        try
                        {
                            string targetPath = Path.Combine(hiddenDir, dirName);
                            if (Directory.Exists(targetPath))
                            {
                                // If target exists, add timestamp
                                targetPath = Path.Combine(hiddenDir, dirName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                            }
                            
                            Directory.Move(dir, targetPath);
                            Response.Write("[OK] Moved: " + dirName + " -> _hidden/" + Path.GetFileName(targetPath) + "\n");
                            movedCount++;
                        }
                        catch (Exception ex)
                        {
                            Response.Write("[ERROR] Error moving " + dirName + ": " + ex.Message + "\n");
                        }
                    }
                    
                    Response.Write("[OK] Moved " + movedCount + " analysis folders to hidden directory\n");
                }
            }
            
            Response.Write("\n=== RESET COMPLETE ===\n");
            Response.Write("[OK] Server-side queue cleared\n");
            Response.Write("[OK] Analysis folders hidden from UI\n");
            Response.Write("[INFO] All files are preserved - nothing was deleted\n");
            Response.Write("[INFO] Refresh the main page to see the empty list\n");
        }
        catch (Exception ex)
        {
            Response.Write("[ERROR] Error resetting analysis list: " + ex.Message + "\n");
        }
    }

    void HandleRefreshListRequest()
    {
        Response.ContentType = "text/plain";
        Response.Write("=== REFRESHING ANALYSIS LIST ===\n\n");
        
        try
        {
            // Step 1: Restore analysis folders
            Response.Write("Step 1: Restoring hidden analysis folders...\n");
            string analysisDir = Server.MapPath("~/analysis");
            string hiddenDir = Path.Combine(analysisDir, "_hidden");
            
            var restoredCount = 0;
            if (!Directory.Exists(hiddenDir))
            {
                Response.Write("ⓘ No hidden analysis folders found\n");
            }
            else
            {
                var hiddenDirectories = Directory.GetDirectories(hiddenDir);
                
                if (hiddenDirectories.Length == 0)
                {
                    Response.Write("ⓘ Hidden directory is empty\n");
                }
                else
                {
                    foreach (string hiddenDirPath in hiddenDirectories)
                    {
                        string dirName = Path.GetFileName(hiddenDirPath);
                        
                        try
                        {
                            string targetPath = Path.Combine(analysisDir, dirName);
                            if (Directory.Exists(targetPath))
                            {
                                Response.Write("⚠ Skipped " + dirName + " (already exists in main directory)\n");
                                continue;
                            }
                            
                            Directory.Move(hiddenDirPath, targetPath);
                            Response.Write("✓ Restored: " + dirName + "\n");
                            restoredCount++;
                        }
                        catch (Exception ex)
                        {
                            Response.Write("✗ Error restoring " + dirName + ": " + ex.Message + "\n");
                        }
                    }
                    
                    // Clean up empty hidden directory
                    try
                    {
                        if (Directory.GetDirectories(hiddenDir).Length == 0 && Directory.GetFiles(hiddenDir).Length == 0)
                        {
                            Directory.Delete(hiddenDir);
                            Response.Write("✓ Removed empty hidden directory\n");
                        }
                    }
                    catch { }
                }
            }
            
            // Step 2: Rebuild queue from restored folders
            Response.Write("\nStep 2: Rebuilding queue from analysis folders...\n");
            string queueFile = Server.MapPath("~/App_Data/queue.json");
            
            try
            {
                var restoredDirs = Directory.GetDirectories(analysisDir).Where(d => !Path.GetFileName(d).StartsWith("_")).ToArray();
                var queueJobs = new List<object>();
                
                foreach (string dir in restoredDirs)
                {
                    string dirName = Path.GetFileName(dir);
                    try
                    {
                        // Create a completed queue job for each analysis folder
                        var job = new
                        {
                            id = System.Guid.NewGuid().ToString(),
                            name = dirName,
                            status = "completed",
                            created = Directory.GetCreationTime(dir).ToString("yyyy-MM-dd HH:mm:ss"),
                            updated = Directory.GetLastWriteTime(dir).ToString("yyyy-MM-dd HH:mm:ss"),
                            filePath = ""
                        };
                        queueJobs.Add(job);
                    }
                    catch (Exception ex)
                    {
                        Response.Write("⚠ Could not create queue entry for " + dirName + ": " + ex.Message + "\n");
                    }
                }
                
                var newQueue = new { jobs = queueJobs.ToArray() };
                var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                File.WriteAllText(queueFile, serializer.Serialize(newQueue));
                Response.Write("✓ Rebuilt queue with " + queueJobs.Count + " entries\n");
            }
            catch (Exception ex)
            {
                Response.Write("✗ Error rebuilding queue: " + ex.Message + "\n");
            }
            
            Response.Write("\n=== REFRESH COMPLETE ===\n");
            Response.Write("✓ Restored " + restoredCount + " analysis folders\n");
            Response.Write("✓ Queue rebuilt from analysis folders\n");
            Response.Write("ⓘ Refresh the main page to see the restored list\n");
        }
        catch (Exception ex)
        {
            Response.Write("✗ Error refreshing analysis list: " + ex.Message + "\n");
        }
    }

    void HandleDeleteFailedRequest()
    {
        Response.ContentType = "text/plain";
        Response.Write("=== DELETING FAILED ANALYSIS ENTRIES ===\n\n");
        
        var deletedQueueCount = 0;
        var deletedFolderCount = 0;
        
        try
        {
            // Step 1: Remove failed jobs from queue
            Response.Write("Step 1: Removing failed jobs from queue...\n");
            string queueFile = Server.MapPath("~/App_Data/queue.json");
            
            if (File.Exists(queueFile))
            {
                try
                {
                    string json = File.ReadAllText(queueFile);
                    var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                    var queueData = serializer.DeserializeObject(json) as Dictionary<string, object>;
                    
                    if (queueData != null && queueData.ContainsKey("jobs"))
                    {
                        var jobs = queueData["jobs"] as object[];
                        if (jobs != null)
                        {
                            var keptJobs = new List<object>();
                            var failedJobNames = new List<string>();
                            
                            foreach (var jobObj in jobs)
                            {
                                var job = jobObj as Dictionary<string, object>;
                                if (job != null && job.ContainsKey("status"))
                                {
                                    string status = job["status"].ToString().ToLower();
                                    if (status == "error" || status == "failed")
                                    {
                                        deletedQueueCount++;
                                        if (job.ContainsKey("name"))
                                        {
                                            failedJobNames.Add(job["name"].ToString());
                                        }
                                        Response.Write("[REMOVED] Queue job: " + (job.ContainsKey("name") ? job["name"].ToString() : "unknown") + " (status: " + status + ")\n");
                                    }
                                    else
                                    {
                                        keptJobs.Add(job);
                                    }
                                }
                                else
                                {
                                    keptJobs.Add(job); // Keep jobs without status
                                }
                            }
                            
                            // Update queue with only successful jobs
                            var updatedQueue = new { jobs = keptJobs.ToArray() };
                            File.WriteAllText(queueFile, serializer.Serialize(updatedQueue));
                            Response.Write("[OK] Removed " + deletedQueueCount + " failed jobs from queue\n");
                            
                            // Step 2: Hide corresponding analysis folders
                            Response.Write("\nStep 2: Hiding folders for failed analyses...\n");
                            string analysisDir = Server.MapPath("~/analysis");
                            
                            if (Directory.Exists(analysisDir) && failedJobNames.Count > 0)
                            {
                                string hiddenDir = Path.Combine(analysisDir, "_hidden_failed");
                                if (!Directory.Exists(hiddenDir))
                                {
                                    Directory.CreateDirectory(hiddenDir);
                                    Response.Write("[OK] Created hidden directory: " + hiddenDir + "\n");
                                }
                                
                                foreach (string jobName in failedJobNames)
                                {
                                    string jobDir = Path.Combine(analysisDir, jobName);
                                    if (Directory.Exists(jobDir))
                                    {
                                        try
                                        {
                                            string targetPath = Path.Combine(hiddenDir, jobName);
                                            if (Directory.Exists(targetPath))
                                            {
                                                targetPath = Path.Combine(hiddenDir, jobName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                                            }
                                            
                                            Directory.Move(jobDir, targetPath);
                                            Response.Write("[OK] Moved failed analysis: " + jobName + " -> _hidden_failed/\n");
                                            deletedFolderCount++;
                                        }
                                        catch (Exception ex)
                                        {
                                            Response.Write("[ERROR] Error moving " + jobName + ": " + ex.Message + "\n");
                                        }
                                    }
                                    else
                                    {
                                        Response.Write("[INFO] No folder found for failed job: " + jobName + "\n");
                                    }
                                }
                            }
                            else
                            {
                                Response.Write("[INFO] No failed job folders to hide\n");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Response.Write("[ERROR] Error processing queue: " + ex.Message + "\n");
                }
            }
            else
            {
                Response.Write("[INFO] Queue file does not exist\n");
            }
            
            Response.Write("\n=== DELETE FAILED COMPLETE ===\n");
            Response.Write("[OK] Removed " + deletedQueueCount + " failed queue entries\n");
            Response.Write("[OK] Hidden " + deletedFolderCount + " failed analysis folders\n");
            Response.Write("[INFO] Only successful analyses remain visible\n");
            Response.Write("[INFO] Refresh the main page to see the updated list\n");
        }
        catch (Exception ex)
        {
            Response.Write("[ERROR] Error deleting failed entries: " + ex.Message + "\n");
        }
    }

    void HandleDeleteJobRequest()
    {
        Response.ContentType = "text/plain";
        string jobId = Request.QueryString["jobid"];
        
        if (string.IsNullOrEmpty(jobId))
        {
            Response.Write("[ERROR] Job ID parameter is required\n");
            return;
        }
        
        Response.Write("=== DELETING SPECIFIC JOB ===\n\n");
        Response.Write("Job ID: " + jobId + "\n\n");
        
        var deletedFromQueue = false;
        var deletedFolderCount = 0;
        string jobName = null;
        
        try
        {
            // Step 1: Remove job from queue
            Response.Write("Step 1: Removing job from queue...\n");
            string queueFile = Server.MapPath("~/App_Data/queue.json");
            
            if (File.Exists(queueFile))
            {
                string json = File.ReadAllText(queueFile);
                var serializer = new JavaScriptSerializer();
                var queueData = serializer.DeserializeObject(json) as Dictionary<string, object>;
                
                if (queueData != null && queueData.ContainsKey("jobs"))
                {
                    var jobsArray = queueData["jobs"] as object[];
                    if (jobsArray != null)
                    {
                        var keptJobs = new List<object>();
                        
                        foreach (var job in jobsArray)
                        {
                            var jobDict = job as Dictionary<string, object>;
                            if (jobDict != null && jobDict.ContainsKey("id"))
                            {
                                string currentJobId = jobDict["id"].ToString();
                                if (currentJobId == jobId)
                                {
                                    // Found the job to delete
                                    deletedFromQueue = true;
                                    if (jobDict.ContainsKey("name"))
                                    {
                                        jobName = jobDict["name"].ToString();
                                    }
                                    Response.Write("[OK] Found job in queue: " + jobName + "\n");
                                }
                                else
                                {
                                    keptJobs.Add(job);
                                }
                            }
                        }
                        
                        if (deletedFromQueue)
                        {
                            // Update queue with job removed
                            var updatedQueue = new { jobs = keptJobs.ToArray() };
                            File.WriteAllText(queueFile, serializer.Serialize(updatedQueue));
                            Response.Write("[OK] Removed job from queue\n");
                        }
                        else
                        {
                            Response.Write("[INFO] Job not found in queue\n");
                        }
                    }
                }
            }
            else
            {
                Response.Write("[INFO] Queue file does not exist\n");
            }
            
            // Step 2: Delete analysis folder if we found the job name
            if (!string.IsNullOrEmpty(jobName))
            {
                Response.Write("\nStep 2: Deleting analysis folder...\n");
                string analysisDir = Server.MapPath("~/analysis");
                string jobFolder = Path.Combine(analysisDir, jobName);
                
                if (Directory.Exists(jobFolder))
                {
                    Directory.Delete(jobFolder, true);
                    deletedFolderCount = 1;
                    Response.Write("[OK] Deleted analysis folder: " + jobName + "\n");
                }
                else
                {
                    Response.Write("[INFO] Analysis folder not found: " + jobName + "\n");
                }
            }
            else
            {
                Response.Write("\nStep 2: Skipping folder deletion (job name not found)\n");
            }
            
            Response.Write("\n=== DELETE JOB COMPLETE ===\n");
            if (deletedFromQueue)
            {
                Response.Write("[SUCCESS] Job " + jobId + " deleted successfully\n");
                Response.Write("[OK] Removed from queue: " + (deletedFromQueue ? "Yes" : "No") + "\n");
                Response.Write("[OK] Deleted folders: " + deletedFolderCount + "\n");
                Response.Write("[INFO] Refresh the main page to see the updated list\n");
            }
            else
            {
                Response.Write("[WARNING] Job " + jobId + " was not found in the queue\n");
                Response.Write("[INFO] The job may have already been deleted or never existed\n");
            }
        }
        catch (Exception ex)
        {
            Response.Write("[ERROR] Error deleting job: " + ex.Message + "\n");
        }
    }

    string FormatFileSize(long bytes)
    {
        if (bytes == 0) return "0 B";
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size = size / 1024;
        }
        return size.ToString("0.##") + " " + sizes[order];
    }
</script>
