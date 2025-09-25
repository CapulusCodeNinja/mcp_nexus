<%@ Page Language="C#" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Web.Script.Serialization" %>
<%@ Import Namespace="System.Diagnostics" %>
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
        if (jobDict != null && jobDict.ContainsKey("status") && jobDict["status"].ToString() == "processing")
        {
            string filePath = jobDict["filePath"].ToString();
            string jobId = jobDict["id"].ToString();
            
            // Check if analysis files were actually created
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            string analysisDir = Server.MapPath("~/analysis");
            string expectedDir = Path.Combine(analysisDir, fileNameWithoutExt);
            
            bool hasAnalysis = false;
            if (Directory.Exists(expectedDir))
            {
                string[] expectedFiles = { fileNameWithoutExt + ".md", "analysis.md", "console.txt", "cdb_analyze.txt" };
                foreach (string expectedFile in expectedFiles)
                {
                    if (File.Exists(Path.Combine(expectedDir, expectedFile)))
                    {
                        hasAnalysis = true;
                        break;
                    }
                }
            }
            
            if (hasAnalysis)
            {
                UpdateJobStatus(jobId, "completed");
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
                // Check if analysis files were actually created
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                string analysisDir = Server.MapPath("~/analysis");
                string expectedDir = Path.Combine(analysisDir, fileNameWithoutExt);
                
                bool hasAnalysis = false;
                if (Directory.Exists(expectedDir))
                {
                    string[] expectedFiles = { fileNameWithoutExt + ".md", "analysis.md", "console.txt", "cdb_analyze.txt" };
                    foreach (string expectedFile in expectedFiles)
                    {
                        if (File.Exists(Path.Combine(expectedDir, expectedFile)))
                        {
                            hasAnalysis = true;
                            break;
                        }
                    }
                }
                
                if (hasAnalysis)
                {
                    UpdateJobStatus(jobId, "completed");
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
                if (error != null)
                {
                    jobDict["error"] = error;
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
