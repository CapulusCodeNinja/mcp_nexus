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
                                    
                                    // Check for analysis report (MD file) - prefer clean naming
                                    enhancedJob["hasAnalysis"] = File.Exists(Path.Combine(analysisDir, "analysis.md")) || 
                                                                File.Exists(Path.Combine(analysisDir, jobName + ".md"));
                                    
                                    // Check for WinDbg output
                                    enhancedJob["hasWinDbg"] = File.Exists(Path.Combine(analysisDir, "cdb_analyze.txt"));
                                    
                                    // Check for console log
                                    enhancedJob["hasConsole"] = File.Exists(Path.Combine(analysisDir, "console.txt"));
                                    
                                    // Check for dump file (should always be true for completed jobs)
                                    enhancedJob["hasDump"] = Directory.GetFiles(analysisDir, "*.dmp").Length > 0;
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

