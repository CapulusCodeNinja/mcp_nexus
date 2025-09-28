<%@ Page Language="C#" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Web.Script.Serialization" %>
<%@ Import Namespace="System.Diagnostics" %>
<%@ Import Namespace="System.Linq" %>
<%
    // Simple queue processor that directly processes files
    Response.ContentType = "text/plain";
    
    try 
    {
        ProcessQueue();
        Response.Write("Queue processing initiated");
    }
    catch (Exception ex)
    {
        Response.Write("Error: " + ex.Message);
    }
%>

<script runat="server">
void ProcessQueue()
{
    string queueFile = Server.MapPath("~/App_Data/queue.json");
    
    if (!File.Exists(queueFile)) return;
    
    string json = File.ReadAllText(queueFile);
    var serializer = new JavaScriptSerializer();
    var queueData = serializer.DeserializeObject(json) as Dictionary<string, object>;
    
    if (queueData == null || !queueData.ContainsKey("jobs")) return;
    
    var jobsArray = queueData["jobs"] as object[];
    if (jobsArray == null) return;
    
    List<object> jobs = new List<object>();
    foreach (var job in jobsArray)
    {
        jobs.Add(job);
    }
    
    // Check for completed processing jobs first
    for (int i = 0; i < jobs.Count; i++)
    {
        var jobDict = jobs[i] as Dictionary<string, object>;
        if (jobDict != null && jobDict.ContainsKey("status") && 
            (jobDict["status"].ToString() == "processing" || 
             (jobDict["status"].ToString() == "completed" && jobDict.ContainsKey("note"))))
        {
            string filePath = jobDict["filePath"].ToString();
            string jobId = jobDict["id"].ToString();
            
            // Check if analysis files were actually created
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            string analysisDir = Server.MapPath("~/analysis");
            string expectedDir = Path.Combine(analysisDir, fileNameWithoutExt);
            
            bool hasAnalysis = false;
            string actualDir = null;
            
            // Try exact match first
            if (Directory.Exists(expectedDir))
            {
                actualDir = expectedDir;
            }
            else
            {
                // Handle timing mismatches - look for directories with similar pattern
                if (Directory.Exists(analysisDir))
                {
                    string basePattern = fileNameWithoutExt;
                    if (fileNameWithoutExt.Length >= 19 && fileNameWithoutExt.StartsWith("dump_"))
                    {
                        // For dump_20250928_193822 -> look for dump_20250928_1938*
                        basePattern = fileNameWithoutExt.Substring(0, 19); // dump_20250928_1938
                    }
                    else if (fileNameWithoutExt.Length >= 17)
                    {
                        basePattern = fileNameWithoutExt.Substring(0, 17);
                    }
                    
                    var matchingDirs = Directory.GetDirectories(analysisDir)
                        .Where(d => Path.GetFileName(d).StartsWith(basePattern))
                        .OrderByDescending(d => Directory.GetLastWriteTime(d))
                        .ToArray();
                    
                    if (matchingDirs.Length > 0)
                    {
                        actualDir = matchingDirs[0];
                    }
                }
            }
            
            if (actualDir != null)
            {
                string actualDirName = Path.GetFileName(actualDir);
                string[] expectedFiles = { fileNameWithoutExt + ".md", actualDirName + ".md", "analysis.md", "console.txt", "cdb_analyze.txt" };
                foreach (string expectedFile in expectedFiles)
                {
                    if (File.Exists(Path.Combine(actualDir, expectedFile)))
                    {
                        hasAnalysis = true;
                        break;
                    }
                }
            }
            
            if (hasAnalysis)
            {
                // Check if this is a full analysis (has MD file) or partial
                bool hasFullAnalysis = false;
                if (actualDir != null)
                {
                    string actualDirName = Path.GetFileName(actualDir);
                    string[] fullAnalysisFiles = { fileNameWithoutExt + ".md", actualDirName + ".md", "analysis.md" };
                    foreach (string file in fullAnalysisFiles)
                    {
                        if (File.Exists(Path.Combine(actualDir, file)))
                        {
                            hasFullAnalysis = true;
                            break;
                        }
                    }
                }
                
                if (hasFullAnalysis)
                {
                    // Full analysis available - clear any previous notes
                    UpdateJobStatus(jobId, "completed", null);
                }
                else
                {
                    // Only partial analysis available
                    UpdateJobStatus(jobId, "completed", "Partial analysis - AI component failed but WinDbg analysis completed successfully");
                }
            }
        }
    }
    
    // Find first pending job to start processing
    for (int i = 0; i < jobs.Count; i++)
    {
        var jobDict = jobs[i] as Dictionary<string, object>;
        if (jobDict != null && jobDict.ContainsKey("status") && jobDict["status"].ToString() == "pending")
        {
            // Update status to processing
            jobDict["status"] = "processing";
            jobDict["updated"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            // Save updated queue
            var updatedQueueData = new { jobs = jobs };
            File.WriteAllText(queueFile, serializer.Serialize(updatedQueueData));
            
            // Process this job in background
            string filePath = jobDict["filePath"].ToString();
            string jobId = jobDict["id"].ToString();
            
            System.Threading.ThreadPool.QueueUserWorkItem(_ => {
                ProcessDumpFileSimple(filePath, jobId);
            });
            
            break;
        }
    }
}

void ProcessDumpFileSimple(string filePath, string jobId)
{
    try
    {
        // Create a simple HTTP request to upload.aspx with proper multipart form data
        string boundary = "----WebKitFormBoundary" + DateTime.Now.Ticks.ToString("x");
        
        // Read the file
        byte[] fileData = File.ReadAllBytes(filePath);
        string fileName = Path.GetFileName(filePath);
        
        // Build multipart form data
        var formData = new System.Text.StringBuilder();
        formData.AppendLine("--" + boundary);
        formData.AppendLine("Content-Disposition: form-data; name=\"dumpFile\"; filename=\"" + fileName + "\"");
        formData.AppendLine("Content-Type: application/octet-stream");
        formData.AppendLine();
        
        byte[] formDataBytes = System.Text.Encoding.UTF8.GetBytes(formData.ToString());
        byte[] endBoundaryBytes = System.Text.Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");
        
        // Combine all parts
        byte[] postData = new byte[formDataBytes.Length + fileData.Length + endBoundaryBytes.Length];
        System.Buffer.BlockCopy(formDataBytes, 0, postData, 0, formDataBytes.Length);
        System.Buffer.BlockCopy(fileData, 0, postData, formDataBytes.Length, fileData.Length);
        System.Buffer.BlockCopy(endBoundaryBytes, 0, postData, formDataBytes.Length + fileData.Length, endBoundaryBytes.Length);
        
        // Create HTTP request
        var request = System.Net.WebRequest.Create("http://localhost/upload.aspx") as System.Net.HttpWebRequest;
        request.Method = "POST";
        request.ContentType = "multipart/form-data; boundary=" + boundary;
        request.ContentLength = postData.Length;
        request.Timeout = 600000; // 10 minutes timeout to match upload.aspx processing time
        request.ReadWriteTimeout = 600000; // 10 minutes for reading response
        
        // Send request
        using (var requestStream = request.GetRequestStream())
        {
            requestStream.Write(postData, 0, postData.Length);
        }
        
        // Get response
        using (var response = request.GetResponse())
        using (var responseStream = response.GetResponseStream())
        using (var reader = new StreamReader(responseStream))
        {
            string result = reader.ReadToEnd();
            
            if (result.Contains("ERROR"))
            {
                UpdateJobStatus(jobId, "error", result);
            }
            else
            {
                // Check if analysis files were actually created (handle timing mismatches)
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                string analysisDir = Server.MapPath("~/analysis");
                
                bool hasAnyFiles = false;
                bool hasAnalysisMd = false;
                string actualDir = null;
                
                // Try exact match first
                string expectedDir = Path.Combine(analysisDir, fileNameWithoutExt);
                if (Directory.Exists(expectedDir))
                {
                    actualDir = expectedDir;
                }
                else
                {
                    // Handle timing mismatches - look for directories with similar pattern
                    if (Directory.Exists(analysisDir))
                    {
                        string basePattern = fileNameWithoutExt.Length >= 17 ? fileNameWithoutExt.Substring(0, 17) : fileNameWithoutExt;
                        var matchingDirs = Directory.GetDirectories(analysisDir)
                            .Where(d => Path.GetFileName(d).StartsWith(basePattern))
                            .OrderByDescending(d => Directory.GetLastWriteTime(d))
                            .ToArray();
                        
                        if (matchingDirs.Length > 0)
                        {
                            actualDir = matchingDirs[0];
                        }
                    }
                }
                
                if (actualDir != null)
                {
                    // Check for any analysis files
                    string[] analysisFiles = { "analysis.md", fileNameWithoutExt + ".md" };
                    string[] otherFiles = { "console.txt", "cdb_analyze.txt", "dump.dmp" };
                    
                    foreach (string file in analysisFiles)
                    {
                        if (File.Exists(Path.Combine(actualDir, file)))
                        {
                            hasAnalysisMd = true;
                            hasAnyFiles = true;
                            break;
                        }
                    }
                    
                    foreach (string file in otherFiles)
                    {
                        if (File.Exists(Path.Combine(actualDir, file)))
                        {
                            hasAnyFiles = true;
                            break;
                        }
                    }
                }
                
                if (hasAnyFiles)
                {
                    if (hasAnalysisMd)
                    {
                        UpdateJobStatus(jobId, "completed");
                    }
                    else
                    {
                        UpdateJobStatus(jobId, "completed", "Partial analysis - AI component failed but WinDbg analysis completed successfully");
                    }
                }
                else
                {
                    UpdateJobStatus(jobId, "error", "Analysis completed but no output files were generated. Upload response: " + result.Substring(0, Math.Min(200, result.Length)));
                }
            }
        }
    }
    catch (Exception ex)
    {
        UpdateJobStatus(jobId, "error", ex.Message);
    }
}

void UpdateJobStatus(string jobId, string status, string error = null)
{
    try
    {
        string queueFile = Server.MapPath("~/App_Data/queue.json");
        
        if (!File.Exists(queueFile)) return;
        
        string json = File.ReadAllText(queueFile);
        var serializer = new JavaScriptSerializer();
        var queueData = serializer.DeserializeObject(json) as Dictionary<string, object>;
        
        if (queueData == null || !queueData.ContainsKey("jobs")) return;
        
        var jobsArray = queueData["jobs"] as object[];
        if (jobsArray == null) return;
        
        List<object> jobs = new List<object>();
        foreach (var job in jobsArray)
        {
            var jobDict = job as Dictionary<string, object>;
            if (jobDict != null && jobDict.ContainsKey("id") && jobDict["id"].ToString() == jobId)
            {
                jobDict["status"] = status;
                jobDict["updated"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                
                if (status == "completed")
                {
                    if (error != null)
                    {
                        // For completed jobs with messages, use "note" instead of "error"
                        jobDict["note"] = error;
                    }
                    else
                    {
                        // Clear any existing notes for fully completed jobs
                        if (jobDict.ContainsKey("note"))
                        {
                            jobDict.Remove("note");
                        }
                    }
                    // Always remove error for completed jobs
                    if (jobDict.ContainsKey("error"))
                    {
                        jobDict.Remove("error");
                    }
                }
                else if (error != null)
                {
                    jobDict["error"] = error;
                    if (jobDict.ContainsKey("note"))
                    {
                        jobDict.Remove("note");
                    }
                }
            }
            jobs.Add(job);
        }
        
        var updatedQueueData = new { jobs = jobs };
        File.WriteAllText(queueFile, serializer.Serialize(updatedQueueData));
        
        // Process next job if this one completed
        if (status == "completed" || status == "error")
        {
            System.Threading.ThreadPool.QueueUserWorkItem(_ => {
                System.Threading.Thread.Sleep(1000); // Brief delay
                ProcessQueue();
            });
        }
    }
    catch (Exception ex)
    {
        // Log error but don't throw
        System.Diagnostics.Debug.WriteLine("UpdateJobStatus error: " + ex.Message);
    }
}
</script>
