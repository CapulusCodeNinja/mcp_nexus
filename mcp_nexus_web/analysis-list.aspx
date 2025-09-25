<%@ Page Language="C#" Debug="true" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="System.Web.Script.Serialization" %>

<script runat="server">
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "application/json";
        try
        {
            string dir = Server.MapPath("~/analysis");
            if (!Directory.Exists(dir)) { Response.Write("[]"); return; }

            var folders = new DirectoryInfo(dir).GetDirectories()
                .Where(d => !d.Name.StartsWith("work_")) // Filter out temporary work directories
                .OrderByDescending(d => d.LastWriteTimeUtc)
                .Select(d => {
                    var files = new System.Collections.Generic.Dictionary<string, object>();
                    
                    try {
                        // Check for MD file (should match directory name)
                        var mdFile = d.GetFiles(d.Name + ".md").FirstOrDefault();
                        if (mdFile == null) {
                            // Fallback to analysis.md for existing files
                            mdFile = d.GetFiles("analysis.md").FirstOrDefault();
                        }
                    if (mdFile != null) {
                        files["analysis"] = new System.Collections.Generic.Dictionary<string, object> {
                            {"size", mdFile.Length},
                            {"modified", mdFile.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")},
                            {"url", "analysis/" + d.Name + "/" + mdFile.Name}
                        };
                    }
                    
                    // Check for console.txt
                    var consoleFile = d.GetFiles("console.txt").FirstOrDefault();
                    if (consoleFile != null) {
                        files["console"] = new System.Collections.Generic.Dictionary<string, object> {
                            {"size", consoleFile.Length},
                            {"modified", consoleFile.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")},
                            {"url", "analysis/" + d.Name + "/console.txt"}
                        };
                    }
                    
                    // Check for cdb_analyze.txt
                    var cdbFile = d.GetFiles("cdb_analyze.txt").FirstOrDefault();
                    if (cdbFile != null) {
                        files["cdb_analyze"] = new System.Collections.Generic.Dictionary<string, object> {
                            {"size", cdbFile.Length},
                            {"modified", cdbFile.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")},
                            {"url", "analysis/" + d.Name + "/cdb_analyze.txt"}
                        };
                    }
                    
                    // Check for dump file
                    var dumpFile = d.GetFiles("*.dmp").FirstOrDefault();
                    if (dumpFile != null) {
                        files["dump"] = new System.Collections.Generic.Dictionary<string, object> {
                            {"name", dumpFile.Name},
                            {"size", dumpFile.Length},
                            {"modified", dumpFile.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")},
                            {"url", "analysis/" + d.Name + "/" + dumpFile.Name}
                        };
                    }
                    
                    } catch {
                        // Skip directories with access issues
                    }
                    
                    return new System.Collections.Generic.Dictionary<string, object> {
                        {"name", d.Name},
                        {"modified", d.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")},
                        {"files", files}
                    };
                }).ToList();

            var ser = new JavaScriptSerializer();
            string json = ser.Serialize(folders);
            Response.Write(json);
        }
        catch (Exception ex)
        {
            Response.Write("{\"error\":\"" + ex.Message.Replace("\"","\\\"") + "\"}");
        }
    }
</script>


