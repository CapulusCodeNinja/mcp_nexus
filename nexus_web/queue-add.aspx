<%@ Page Language="C#" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Web.Script.Serialization" %>
<%
    Response.ContentType = "application/json";
    
    try 
    {
        if (Request.HttpMethod != "POST")
        {
            throw new Exception("Only POST method allowed");
        }
        
        HttpPostedFile uploadedFile = Request.Files["dumpFile"];
        if (uploadedFile == null || uploadedFile.ContentLength == 0)
        {
            throw new Exception("No file uploaded");
        }
        
        // Generate job ID and timestamp
        string jobId = Guid.NewGuid().ToString();
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string jobName = "dump_" + timestamp;
        
        // Save uploaded file
        string uploadsDir = Server.MapPath("~/uploads");
        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }
        
        string fileName = jobName + ".dmp";
        string filePath = Path.Combine(uploadsDir, fileName);
        uploadedFile.SaveAs(filePath);
        
        // Add job to queue
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
                    foreach (var job in jobsArray)
                    {
                        jobs.Add(job);
                    }
                }
            }
        }
        
        // Add new job
        var newJob = new {
            id = jobId,
            name = jobName,
            fileName = fileName,
            filePath = filePath,
            status = "pending",
            created = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            updated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        
        jobs.Insert(0, newJob); // Add to front of queue
        
        // Save queue
        var queueData2 = new { jobs = jobs };
        var serializer2 = new JavaScriptSerializer();
        File.WriteAllText(queueFile, serializer2.Serialize(queueData2));
        
        // Start background processing
        System.Threading.ThreadPool.QueueUserWorkItem(_ => {
            try 
            {
                // Call the simple queue processor
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-Command \"Invoke-WebRequest -Uri 'http://localhost/process.aspx' -Method GET\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    process.WaitForExit(5000);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the response
                System.Diagnostics.Debug.WriteLine("Background processing error: " + ex.Message);
            }
        });
        
        var result = new { success = true, jobId = jobId, jobName = jobName };
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

