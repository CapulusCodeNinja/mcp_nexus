<%@ Page Language="C#" Debug="true" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Diagnostics" %>
<%@ Import Namespace="System.Web" %>
<%@ Import Namespace="System.Linq" %>
 

<script runat="server">
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/plain";
        
        // Configuration - File size limits and memory management
        const long maxFileSize = 2L * 1024 * 1024 * 1024; // 2GB limit to prevent memory issues
        const int streamBufferSize = 8192; // 8KB buffer for streaming operations
        const int maxConcurrentUploads = 5; // Limit concurrent uploads to prevent memory overload
        
        try
        {
            // Check if this is a POST request with files
            if (Request.HttpMethod != "POST")
            {
                Response.Write("ERROR: Only POST requests are allowed");
                return;
            }

            // Create uploads directory if it doesn't exist
            string uploadsPath = Server.MapPath("~/uploads");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // Process uploaded files
            var uploadedFiles = new List<string>();
            var commandOutputs = new List<string>();
            
            foreach (string fileKey in Request.Files)
            {
                HttpPostedFile file = Request.Files[fileKey];
                
                if (file != null && file.ContentLength > 0)
                {
                    // Check file size limit (2GB to prevent memory issues)
                    
                    if (file.ContentLength > maxFileSize)
                    {
                        commandOutputs.Add("ERROR: File '" + file.FileName + "' is too large. " +
                            "Maximum size is 2GB. File size: " + 
                            (file.ContentLength / (1024.0 * 1024.0)).ToString("F1") + " MB");
                        continue;
                    }
                    
                    // Generate standardized filename: dump_<timestamp>.<ext>
                    string fileExtension = Path.GetExtension(file.FileName);
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string uniqueFileName = "dump_" + timestamp + fileExtension;
                    
                    string filePath = Path.Combine(uploadsPath, uniqueFileName);
                    
                    // Save the file using streaming to avoid memory issues
                    try
                    {
                        using (var fileStream = File.Create(filePath))
                        {
                            var buffer = new byte[streamBufferSize]; // Configurable buffer for streaming
                            int bytesRead;
                            while ((bytesRead = file.InputStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                fileStream.Write(buffer, 0, bytesRead);
                            }
                        }
                        
                        uploadedFiles.Add(filePath);
                        
                        // Log the upload
                        LogUpload(file.FileName, uniqueFileName, file.ContentLength);
                        
                        // Analyze file and collect output
                        string analysisOutput = TriggerWSLCommand(filePath);
                        commandOutputs.Add("File: " + uniqueFileName + "\n" + analysisOutput);
                    }
                    catch (Exception ex)
                    {
                        commandOutputs.Add("ERROR: Failed to save file '" + file.FileName + "': " + ex.Message);
                    }
                }
            }

            if (uploadedFiles.Count > 0)
            {
                // Return structured response with file info and command outputs
                string responseData = "SUCCESS|" + uploadedFiles.Count + " file(s) uploaded and processed\n\n";
                responseData += "COMMAND OUTPUTS:\n";
                responseData += "================\n\n";
                
                for (int i = 0; i < commandOutputs.Count; i++)
                {
                    responseData += commandOutputs[i];
                    if (i < commandOutputs.Count - 1)
                    {
                        responseData += "\n" + new string('-', 50) + "\n\n";
                    }
                }
                
                Response.Write(responseData);
            }
            else
            {
                Response.Write("ERROR: No files were uploaded");
            }
        }
        catch (Exception ex)
        {
            // Log the error
            LogError(ex);
            Response.Write("ERROR: " + ex.Message);
        }
    }
    
    private string TriggerWSLCommand(string filePath)
    {
        try
        {
            // Always run synchronously using the template payload
            string wslResult = RunCursorAgentWithTemplate(filePath);
            if (wslResult.StartsWith("CURSOR-AGENT SUCCESS:"))
            {
                LogApplicationResult(filePath, 0, wslResult, "");
                return wslResult;
            }
            else
            {
                // No fallback block requested; return WSL status only
                LogApplicationResult(filePath, -1, wslResult, "");
                return wslResult;
            }
        }
        catch (Exception ex)
        {
            string errorMsg = "Failed to analyze file: " + filePath + " - " + ex.Message;
            LogError(ex, errorMsg);
            return "ERROR: " + errorMsg;
        }
    }
    
    
    private string EscapeForSingleQuotedBash(string input)
    {
        if (input == null) return "";
        // Safely embed arbitrary text inside single quotes for bash: 'foo' -> 'foo'; abc'def -> 'abc'\''def'
        return input.Replace("'", "'\\''");
    }

    private string RunCursorAgentWithTemplate(string windowsFilePath)
    {
        string workWin = null;
        string workWsl = null;
        try
        {
            string templatePath = Server.MapPath("~/crash-anaylse-command.md");
            string templatePathAlt = Server.MapPath("~/crash-analyze-command.md");
            if (!File.Exists(templatePath))
            {
                if (File.Exists(templatePathAlt))
                {
                    templatePath = templatePathAlt;
                }
                else
                {
                    return "Template not found: expected crash-anaylse-command.md or crash-analyze-command.md in site root.";
                }
            }
            string template = File.ReadAllText(templatePath);
            // Replace minimal placeholders requested by user
            // IMPORTANT: Do not perform any other replacements
            // We only compute siteRootWsl for setting the working directory before running cursor-agent
            string siteRootWin = Server.MapPath("~");
            string siteRootWsl = ConvertToWSLPath(siteRootWin);
            if (!siteRootWsl.EndsWith("/")) siteRootWsl += "/";

            // Prepare a temporary working directory under workingdir
            string workingRootWin = Server.MapPath("~/workingdir");
            if (!Directory.Exists(workingRootWin)) Directory.CreateDirectory(workingRootWin);
            workWin = Path.Combine(workingRootWin, "work_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));
            Directory.CreateDirectory(workWin);
            workWsl = ConvertToWSLPath(workWin);
            if (!workWsl.EndsWith("/")) workWsl += "/";

            // Prepare the analysis directory for final results
            string analysisRootWin = Server.MapPath("~/analysis");
            if (!Directory.Exists(analysisRootWin)) Directory.CreateDirectory(analysisRootWin);

            // Run WinDbg !analyze -v in parallel and write to analysis directory
            try
            {
                string baseNameForAnalyze = Path.GetFileNameWithoutExtension(windowsFilePath);
                string analysisDir = Path.Combine(analysisRootWin, baseNameForAnalyze);
                if (!Directory.Exists(analysisDir))
                {
                    Directory.CreateDirectory(analysisDir);
                }
                string analyzeOutPath = Path.Combine(analysisDir, "cdb_analyze.txt");
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    RunAnalyzeV(windowsFilePath, analyzeOutPath, 1800000); // 30 minutes timeout
                });
            }
            catch { }

            // Use WSL path for [workingdir] so the agent can create files inside Linux
            template = template.Replace("[workingdir]", workWsl.TrimEnd('/'));
            // Use WSL path for [outputdir] - where the AI should save the .md result file (final location)
            string outputBaseName = Path.GetFileNameWithoutExtension(windowsFilePath);
            string finalAnalysisDir = Path.Combine(analysisRootWin, outputBaseName);
            string finalAnalysisDirWsl = ConvertToWSLPath(finalAnalysisDir);
            template = template.Replace("[outputdir]", finalAnalysisDirWsl.TrimEnd('/'));
            // Replace [filename] with full Windows path to the uploaded dump (required by MCP open_windbg_dump)
            template = template.Replace("[filename]", windowsFilePath);

            // Write payload to a temp file and read it to avoid quoting issues
            string tempPayloadWin = Path.Combine(workWin, "payload_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".md");
            File.WriteAllText(tempPayloadWin, template);
            string tempPayloadWsl = ConvertToWSLPath(tempPayloadWin);
            // Prepare a stdout/stderr sink file inside work dir to avoid pipe blocking
            string wslConsoleWin = Path.Combine(workWin, "agent_console_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".txt");
            string wslConsoleWsl = ConvertToWSLPath(wslConsoleWin);

            // Set HOME environment and run cursor-agent with clean output
            string cmd = "export HOME=/home/droller && cd '$HOME' && cursor-agent -f --output-format text < '" + tempPayloadWsl + "' > '" + wslConsoleWsl + "' 2>&1";

            var psi = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = "-u droller bash -l -c \"" + cmd.Replace("\"", "\\\"") + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = true
            };

            using (var p = Process.Start(psi))
            {
                // Poll for result and stop when MD exists - configurable timeout via environment variable
                string foundWin = null;
                var startTime = DateTime.UtcNow;
                
                // Check if max runtime is specified (0 = unlimited)
                string envMaxRuntime = Environment.GetEnvironmentVariable("CURSOR_AGENT_MAX_RUNTIME_MINUTES");
                int maxRuntimeMinutes = 0; // Default: unlimited
                int customRuntime;
                if (!string.IsNullOrEmpty(envMaxRuntime) && int.TryParse(envMaxRuntime, out customRuntime))
                {
                    maxRuntimeMinutes = customRuntime;
                }
                
                while (!p.HasExited)
                {
                    // Look for analysis.md file (simplified naming)
                    string baseName = Path.GetFileNameWithoutExtension(windowsFilePath);
                    string pick = null;
                    
                    // Check locations in priority order for analysis.md or dump-named .md files
                    string expectedInWorkSub = Path.Combine(Path.Combine(workWin, "analysis"), "analysis.md");
                    string expectedInWork = Path.Combine(workWin, "analysis.md");
                    string expectedInRoot = Path.Combine(analysisRootWin, "analysis.md");
                    string expectedInSubDir = Path.Combine(Path.Combine(analysisRootWin, baseName), "analysis.md");
                    
                    // Also check for dump-named files (e.g., dump_20250928_182405.md)
                    string expectedDumpNamedInSubDir = Path.Combine(Path.Combine(analysisRootWin, baseName), baseName + ".md");
                    string expectedDumpNamedInWork = Path.Combine(workWin, baseName + ".md");
                    string expectedDumpNamedInRoot = Path.Combine(analysisRootWin, baseName + ".md");
                    
                    if (File.Exists(expectedInWorkSub))
                        pick = expectedInWorkSub;
                    else if (File.Exists(expectedInWork))
                        pick = expectedInWork;
                    else if (File.Exists(expectedInSubDir))
                        pick = expectedInSubDir;
                    else if (File.Exists(expectedDumpNamedInSubDir))
                        pick = expectedDumpNamedInSubDir;
                    else if (File.Exists(expectedDumpNamedInWork))
                        pick = expectedDumpNamedInWork;
                    else if (File.Exists(expectedDumpNamedInRoot))
                        pick = expectedDumpNamedInRoot;
                    else if (File.Exists(expectedInRoot))
                        pick = expectedInRoot;

                    if (pick != null)
                    {
                        // Create dedicated directory for this analysis using original job name
                        string analysisDir = Path.Combine(analysisRootWin, baseName);
                        if (!Directory.Exists(analysisDir))
                        {
                            Directory.CreateDirectory(analysisDir);
                        }
                        
                        // Determine destination filename - preserve dump-named files, otherwise use analysis.md
                        string destFileName = "analysis.md";
                        string pickFileName = Path.GetFileName(pick);
                        if (pickFileName.StartsWith(baseName + ".") && pickFileName.EndsWith(".md"))
                        {
                            // Preserve dump-named files (e.g., dump_20250928_182405.md)
                            destFileName = pickFileName;
                        }
                        
                        string dest = Path.Combine(analysisDir, destFileName);
                        if (!string.Equals(pick, dest, StringComparison.OrdinalIgnoreCase))
                        {
                            if (File.Exists(dest))
                            {
                                File.Delete(dest); // Replace existing file
                            }
                            File.Move(pick, dest);
                        }
                        
                        foundWin = dest;
						// Give cursor-agent a moment to finish writing the file
                        System.Threading.Thread.Sleep(3000); // Wait 3 seconds to let it complete output
                        
						// Stop the agent now that we have the result
						try { 
                            p.Kill(); 
                            p.WaitForExit(2000); // Give it 2 seconds to die gracefully
                        } catch { }
                        
                        // Kill all cursor-agent processes
                        KillAllCursorAgentProcesses();
                        break;
                    }
                    
                    // Check for timeout if max runtime is set
                    if (maxRuntimeMinutes > 0)
                    {
                        var elapsed = DateTime.UtcNow - startTime;
                        if (elapsed.TotalMinutes >= maxRuntimeMinutes)
                        {
                            LogApplicationResult(windowsFilePath, -2, "Cursor-agent execution timeout after " + maxRuntimeMinutes + " minutes", "");
                            break;
                        }
                    }
                    
                    System.Threading.Thread.Sleep(1000);
                }

                // Force kill cursor-agent processes if still running after completion detection
                if (!p.HasExited)
                {
                    try { p.Kill(); } catch { }
                }
                
                // Always kill any lingering cursor-agent processes - more aggressive cleanup
                KillAllCursorAgentProcesses();

                string output = "";
                string error = "";

                // Move console sink to analysis directory if present
                try
                {
                    if (File.Exists(wslConsoleWin))
                    {
                        string baseName = Path.GetFileNameWithoutExtension(windowsFilePath);
                        string analysisDir = Path.Combine(analysisRootWin, baseName);
                        if (!Directory.Exists(analysisDir))
                        {
                            Directory.CreateDirectory(analysisDir);
                        }
                        
                        string destConsole = Path.Combine(analysisDir, "console.txt");
                        if (File.Exists(destConsole))
                        {
                            destConsole = Path.Combine(analysisDir, "console_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
                        }
                        File.Move(wslConsoleWin, destConsole);
                    }
                }
                catch { }

                // Move the original dump file to the analysis directory (regardless of analysis success)
                try
                {
                    if (File.Exists(windowsFilePath))
                    {
                        string baseName = Path.GetFileNameWithoutExtension(windowsFilePath);
                        string analysisDir = Path.Combine(analysisRootWin, baseName);
                        if (!Directory.Exists(analysisDir))
                        {
                            Directory.CreateDirectory(analysisDir);
                        }
                        
                        // Use clean filename: dump.dmp
                        string destDump = Path.Combine(analysisDir, "dump.dmp");
                        if (File.Exists(destDump))
                        {
                            File.Delete(destDump); // Replace existing file
                        }
                        File.Move(windowsFilePath, destDump);
                    }
                }
                catch { }

                // Cleanup happens in finally via robust deleter

                if (foundWin != null)
                {
                    return "CURSOR-AGENT SUCCESS:\nWorkingDir: " + workWsl + "\nResult: " + foundWin + "\n" + output;
                }
                if (p.ExitCode == 0)
                {
                    return "CURSOR-AGENT SUCCESS:\nWorkingDir: " + workWsl + "\nResult: (not found)\n" + output;
                }
                return "cursor-agent failed (exit code: " + p.ExitCode + ")\nWorkingDir: " + workWsl + "\nOutput: " + output + "\nError: " + error;
            }
        }
        catch (Exception ex)
        {
            return "WSL/template execution error: " + ex.Message;
        }
        finally
        {
            if (!string.IsNullOrEmpty(workWin))
            {
                try
                {
                    DeleteDirectoryRobust(workWin, workWsl);
                }
                catch { }
            }
        }
    }

    private void RunAnalyzeV(string dumpPathWin, string outputTxtPath, int timeoutMs)
    {
        try
        {
            string cdb = FindCdbPath();
            if (string.IsNullOrEmpty(cdb) || !File.Exists(cdb))
            {
                try { File.WriteAllText(outputTxtPath, "!analyze -v skipped: cdb.exe not found (install Windows Debugging Tools).\r\n"); } catch { }
                TryLogAnalyze("cdb.exe not found; skipping analyze for " + dumpPathWin);
                return;
            }
            string args = "-logo \"" + outputTxtPath + "\" -z \"" + dumpPathWin + "\" -c \"!analyze -v; q\"";
            var psi = new ProcessStartInfo
            {
                FileName = cdb,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = true
            };
            using (var p = Process.Start(psi))
            {
                if (p == null) { try { File.WriteAllText(outputTxtPath, "Failed to start cdb.exe"); } catch { } return; }
                bool finished = p.WaitForExit(timeoutMs);
                if (!finished)
                {
                    try { p.Kill(); } catch { }
                    try { File.AppendAllText(outputTxtPath, "\r\n[Timeout] !analyze -v exceeded " + (timeoutMs/1000) + "s. Consider using larger timeouts for complex dumps."); } catch { }
                }
                TryLogAnalyze("analyze finished for " + dumpPathWin + ", file=" + outputTxtPath);
            }
        }
        catch (Exception ex)
        {
            try { File.WriteAllText(outputTxtPath, "!analyze -v error: " + ex.Message); } catch { }
            TryLogAnalyze("analyze error for " + dumpPathWin + ": " + ex.Message);
        }
    }

    private string FindCdbPath()
    {
        try
        {
            string[] candidates = new string[]
            {
                Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Windows Kits\10\Debuggers\x64\cdb.exe"),
                Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Windows Kits\10\Debuggers\x64\cdb.exe"),
                Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Windows Kits\10\Debuggers\x86\cdb.exe"),
                Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Windows Kits\10\Debuggers\x86\cdb.exe"),
                "cdb.exe"
            };
            foreach (var path in candidates)
            {
                try { if (!string.IsNullOrEmpty(path) && File.Exists(path)) return path; } catch { }
            }
        }
        catch { }
        return null;
    }

    private void TryLogAnalyze(string message)
    {
        try
        {
            string logPath = Server.MapPath("~/logs");
            if (!Directory.Exists(logPath)) Directory.CreateDirectory(logPath);
            string logFile = Path.Combine(logPath, "analyze_" + DateTime.Now.ToString("yyyyMMdd") + ".log");
            File.AppendAllText(logFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - " + message + "\r\n");
        }
        catch { }
    }
    private string AnalyzeFileWithWindowsTools(string filePath)
    {
        try
        {
            var results = new System.Text.StringBuilder();
            FileInfo fileInfo = new FileInfo(filePath);
            
            // Basic file information
            results.AppendLine("=== WINDOWS FILE ANALYSIS ===");
            results.AppendLine("File: " + Path.GetFileName(filePath));
            results.AppendLine("Size: " + fileInfo.Length.ToString("N0") + " bytes");
            results.AppendLine("Created: " + fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"));
            results.AppendLine("Modified: " + fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
            results.AppendLine("");
            
            // File type detection
            string extension = Path.GetExtension(filePath).ToLower();
            switch (extension)
            {
                case ".dmp":
                    results.AppendLine("File Type: Windows Memory Dump (Crash Dump)");
                    results.AppendLine("Description: Contains system memory snapshot at crash time");
                    break;
                case ".exe":
                    results.AppendLine("File Type: Windows Executable");
                    break;
                case ".dll":
                    results.AppendLine("File Type: Dynamic Link Library");
                    break;
                case ".txt":
                    results.AppendLine("File Type: Text File");
                    break;
                default:
                    results.AppendLine("File Type: " + extension + " file");
                    break;
            }
            results.AppendLine("");
            
            // Hash calculation using Windows tools
            string md5Hash = CalculateFileHash(filePath, "MD5");
            string sha1Hash = CalculateFileHash(filePath, "SHA1");
            
            results.AppendLine("=== FILE HASHES ===");
            results.AppendLine("MD5:  " + md5Hash);
            results.AppendLine("SHA1: " + sha1Hash);
            results.AppendLine("");
            
            // Hex dump of first 200 bytes
            results.AppendLine("=== HEX DUMP (First 200 bytes) ===");
            byte[] buffer = new byte[200];
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                int bytesRead = fs.Read(buffer, 0, 200);
                for (int i = 0; i < bytesRead; i += 16)
                {
                    // Offset
                    results.Append(i.ToString("X8") + "  ");
                    
                    // Hex values
                    for (int j = 0; j < 16 && (i + j) < bytesRead; j++)
                    {
                        results.Append(buffer[i + j].ToString("X2") + " ");
                        if (j == 7) results.Append(" ");
                    }
                    
                    // Padding
                    for (int j = bytesRead - i; j < 16; j++)
                    {
                        results.Append("   ");
                        if (j == 7) results.Append(" ");
                    }
                    
                    results.Append(" |");
                    
                    // ASCII representation
                    for (int j = 0; j < 16 && (i + j) < bytesRead; j++)
                    {
                        byte b = buffer[i + j];
                        results.Append((b >= 32 && b <= 126) ? (char)b : '.');
                    }
                    
                    results.AppendLine("|");
                }
            }
            
            // Special analysis for dump files
            if (extension == ".dmp")
            {
                results.AppendLine("");
                results.AppendLine("=== DUMP FILE ANALYSIS ===");
                results.AppendLine(AnalyzeDumpFile(filePath));
            }
            
            return results.ToString();
        }
        catch (Exception ex)
        {
            return "ERROR analyzing file: " + ex.Message;
        }
    }
    
    private string CalculateFileHash(string filePath, string algorithm)
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "certutil.exe",
                Arguments = "-hashfile \"" + filePath + "\" " + algorithm,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using (Process process = Process.Start(startInfo))
            {
                process.WaitForExit(300000); // 5 minutes timeout for hash calculation
                string output = process.StandardOutput.ReadToEnd();
                
                // Parse certutil output to extract hash
                string[] lines = output.Split('\n');
                if (lines.Length > 1)
                {
                    return lines[1].Trim().Replace(" ", "");
                }
            }
        }
        catch
        {
            // Fallback calculation
            using (var hasher = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = hasher.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "");
                }
            }
        }
        return "Unable to calculate";
    }
    
    private string AnalyzeDumpFile(string filePath)
    {
        try
        {
            var results = new System.Text.StringBuilder();
            
            // Read dump file header
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] header = new byte[32];
                fs.Read(header, 0, 32);
                
                // Check for MDMP signature
                if (header[0] == 0x4D && header[1] == 0x44 && header[2] == 0x4D && header[3] == 0x50)
                {
                    results.AppendLine("✓ Valid Windows Minidump (MDMP) format");
                    
                    // Extract version info
                    int version = BitConverter.ToInt32(header, 4);
                    results.AppendLine("Version: " + version.ToString("X"));
                    
                    // Extract stream count
                    int streamCount = BitConverter.ToInt32(header, 8);
                    results.AppendLine("Number of streams: " + streamCount);
                    
                    // Extract timestamp
                    int timestamp = BitConverter.ToInt32(header, 12);
                    DateTime crashTime = new DateTime(1970, 1, 1).AddSeconds(timestamp);
                    results.AppendLine("Crash time: " + crashTime.ToString("yyyy-MM-dd HH:mm:ss UTC"));
                }
                else
                {
                    results.AppendLine("⚠ Not a standard Windows minidump format");
                }
            }
            
            results.AppendLine("");
            results.AppendLine("Note: For detailed crash analysis, use tools like:");
            results.AppendLine("- WinDbg (Windows Debugger)");
            results.AppendLine("- Visual Studio Debugger");
            results.AppendLine("- BlueScreenView");
            
            return results.ToString();
        }
        catch (Exception ex)
        {
            return "Error analyzing dump structure: " + ex.Message;
        }
    }
    
    private string ConvertToWSLPath(string windowsPath)
    {
        try
        {
            // Convert Windows path to WSL path
            // Example: C:\inetpub\wwwroot\uploads\file.txt -> /mnt/c/inetpub/wwwroot/uploads/file.txt
            if (windowsPath.Length >= 3 && windowsPath[1] == ':')
            {
                char driveLetter = char.ToLower(windowsPath[0]);
                string restOfPath = windowsPath.Substring(2).Replace('\\', '/');
                return "/mnt/" + driveLetter + restOfPath;
            }
            
            // If it's already a Unix-style path, return as-is
            return windowsPath.Replace('\\', '/');
        }
        catch
        {
            // Fallback: just replace backslashes with forward slashes
            return windowsPath.Replace('\\', '/');
        }
    }
    
    private void LogUpload(string originalFileName, string savedFileName, int fileSize)
    {
        try
        {
            string logPath = Server.MapPath("~/logs");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }
            
            string logFile = Path.Combine(logPath, "uploads_" + DateTime.Now.ToString("yyyyMMdd") + ".log");
            string logEntry = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - UPLOAD - Original: " + originalFileName + ", Saved: " + savedFileName + ", Size: " + fileSize + " bytes, IP: " + Request.UserHostAddress + "\r\n";
            
            File.AppendAllText(logFile, logEntry);
        }
        catch
        {
            // Ignore logging errors
        }
    }
    
    
    private void LogApplicationResult(string filePath, int exitCode, string output, string error)
    {
        try
        {
            string logPath = Server.MapPath("~/logs");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }
            
            string logFile = Path.Combine(logPath, "application_" + DateTime.Now.ToString("yyyyMMdd") + ".log");
            string logEntry = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - APP_RESULT - File: " + filePath + ", ExitCode: " + exitCode + ", Output: " + output + ", Error: " + error + "\r\n";
            
            File.AppendAllText(logFile, logEntry);
        }
        catch
        {
            // Ignore logging errors
        }
    }
    
    private void LogError(Exception ex, string additionalInfo = "")
    {
        try
        {
            string logPath = Server.MapPath("~/logs");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }
            
            string logFile = Path.Combine(logPath, "errors_" + DateTime.Now.ToString("yyyyMMdd") + ".log");
            string logEntry = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - ERROR - " + additionalInfo + " - " + ex.Message + " - " + ex.StackTrace + "\r\n";
            
            File.AppendAllText(logFile, logEntry);
        }
        catch
        {
            // Ignore logging errors
        }
    }

    private void DeleteDirectoryRobust(string directoryPath, string directoryPathWsl)
    {
        // Try Windows-side deletion with retries, then attempt WSL-side force remove
        if (string.IsNullOrEmpty(directoryPath)) return;
        const int maxAttempts = 3;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, true);
                }
                return;
            }
            catch
            {
                System.Threading.Thread.Sleep(500 * attempt);
            }
        }
        try
        {
            if (!string.IsNullOrEmpty(directoryPathWsl))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = "-u droller bash -l -c \"rm -rf '" + directoryPathWsl.TrimEnd('/') + "'\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using (var p = Process.Start(psi)) { if (p != null) p.WaitForExit(3000); }
            }
        }
        catch { }
    }
    
    private void KillAllCursorAgentProcesses()
    {
        try
        {
            // Kill all cursor-agent processes in WSL more aggressively
            var killPsi = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = "-u droller bash -l -c \"pkill -9 -f 'cursor-agent' || true; pkill -9 -f 'node.*cursor-agent' || true; killall -9 node 2>/dev/null || true\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using (var k = Process.Start(killPsi))
            {
                if (k != null) { k.WaitForExit(5000); }
            }
        }
        catch { }
    }
</script>
