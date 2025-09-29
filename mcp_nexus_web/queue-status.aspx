<%@ Page Language="C#" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Web.Script.Serialization" %>
<%
    Response.ContentType = "application/json";
    
    try 
    {
        string queueFile = Server.MapPath("~/App_Data/queue.json");
        string queueDir = Path.GetDirectoryName(queueFile);
        
        if (!Directory.Exists(queueDir))
        {
            Directory.CreateDirectory(queueDir);
        }
        
        List<object> jobs = new List<object>();
        
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
                    foreach (var jobObj in jobsArray)
                    {
                        var jobDict = jobObj as Dictionary<string, object>;
                        if (jobDict != null)
                        {
                            // Create a new job object with file availability info
                            var enhancedJob = new Dictionary<string, object>(jobDict);
                            
                            // Check for file availability if job is completed
                            if (jobDict.ContainsKey("name") && jobDict.ContainsKey("status"))
                            {
                                string jobName = jobDict["name"].ToString();
                                string status = jobDict["status"].ToString();
                                
                                if (status == "completed")
                                {
                                    string analysisDir = Server.MapPath("~/analysis/" + jobName);
                                    string actualAnalysisDir = null;
                                    
                                    // Try exact match first
                                    if (Directory.Exists(analysisDir))
                                    {
                                        actualAnalysisDir = analysisDir;
                                    }
                                    else
                                    {
                                        // Handle timing mismatches - look for directories with similar pattern
                                        string analysisRootDir = Server.MapPath("~/analysis");
                                        if (Directory.Exists(analysisRootDir))
                                        {
                                            string basePattern = jobName;
                                            if (jobName.Length >= 17 && jobName.StartsWith("dump_"))
                                            {
                                                // For dump_20250928_193822 -> look for dump_20250928_19*
                                                basePattern = jobName.Substring(0, 17); // dump_20250928_19
                                            }
                                            else if (jobName.Length >= 15)
                                            {
                                                basePattern = jobName.Substring(0, 15);
                                            }
                                            
                                            var matchingDirs = Directory.GetDirectories(analysisRootDir)
                                                .Where(d => Path.GetFileName(d).StartsWith(basePattern))
                                                .OrderByDescending(d => Directory.GetLastWriteTime(d))
                                                .ToArray();
                                            
                                            if (matchingDirs.Length > 0)
                                            {
                                                actualAnalysisDir = matchingDirs[0];
                                            }
                                        }
                                    }
                                    
                                    if (actualAnalysisDir != null)
                                    {
                                        string actualDirName = Path.GetFileName(actualAnalysisDir);
                                        
                                        // Check for analysis report (MD file) - prefer clean naming
                                        enhancedJob["hasAnalysis"] = File.Exists(Path.Combine(actualAnalysisDir, "analysis.md")) || 
                                                                    File.Exists(Path.Combine(actualAnalysisDir, jobName + ".md")) ||
                                                                    File.Exists(Path.Combine(actualAnalysisDir, actualDirName + ".md"));
                                        
                                        // Check for WinDbg output
                                        enhancedJob["hasWinDbg"] = File.Exists(Path.Combine(actualAnalysisDir, "cdb_analyze.txt"));
                                        
                                        // Check for console log
                                        enhancedJob["hasConsole"] = File.Exists(Path.Combine(actualAnalysisDir, "console.txt"));
                                        
                                        // Check for dump file (should always be true for completed jobs)
                                        enhancedJob["hasDump"] = Directory.GetFiles(actualAnalysisDir, "*.dmp").Length > 0;
                                    }
                                    else
                                    {
                                        // No analysis directory found
                                        enhancedJob["hasAnalysis"] = false;
                                        enhancedJob["hasWinDbg"] = false;
                                        enhancedJob["hasConsole"] = false;
                                        enhancedJob["hasDump"] = false;
                                    }
                                }
                                else
                                {
                                    enhancedJob["hasAnalysis"] = false;
                                    enhancedJob["hasWinDbg"] = false;
                                    enhancedJob["hasConsole"] = false;
                                    enhancedJob["hasDump"] = false;
                                }
                            }
                            
                            jobs.Add(enhancedJob);
                        }
                        else
                        {
                            jobs.Add(jobObj);
                        }
                    }
                }
            }
        }
        
        var result = new { success = true, jobs = jobs };
        var resultSerializer = new JavaScriptSerializer();
        Response.Write(resultSerializer.Serialize(result));
    }
    catch (Exception ex)
    {
        var error = new { success = false, error = ex.Message };
        var errorSerializer = new JavaScriptSerializer();
        Response.Write(errorSerializer.Serialize(error));
    }
%>

