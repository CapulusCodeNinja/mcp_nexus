# Testing WSL Path Conversion Notifications

## What You Just Saw

The test output above shows **exactly** what you'll see in your MCP server logs when AI agents provide WSL-style paths. Here are the key notification messages:

### ✅ **Dump Path Conversion**
```
[INFO] mcp_nexus.Tools.WindbgTool: Converted WSL path '/mnt/c/inetpub/wwwroot/uploads/dump_20250925_112751.dmp' to Windows path 'C:\inetpub\wwwroot\uploads\dump_20250925_112751.dmp'
```

### ✅ **Symbols Path Conversion**
```
[INFO] mcp_nexus.Tools.WindbgTool: Converted WSL symbols path '/mnt/d/symbols' to Windows path 'D:\symbols'
```

## How to Test in Real MCP Server Usage

### Method 1: HTTP Mode Testing

1. **Start the MCP server in HTTP mode:**
   ```bash
   dotnet run --project mcp_nexus -- --http
   ```

2. **Watch the console output** - you'll see server startup messages

3. **Send a test request** using PowerShell:
   ```powershell
   $body = @{
       jsonrpc = "2.0"
       id = 1
       method = "tools/call"
       params = @{
           name = "nexus_open_dump_analyze_session"
           arguments = @{
               dumpPath = "/mnt/c/inetpub/wwwroot/uploads/test_dump.dmp"
               symbolsPath = "/mnt/d/symbols"
           }
       }
   } | ConvertTo-Json -Depth 10

   Invoke-RestMethod -Uri "http://localhost:5000/mcp" -Method POST -Body $body -ContentType "application/json"
   ```

4. **Check the server console** - you should see the conversion log messages appear!

### Method 2: Cursor/AI Integration Testing

1. **Configure Cursor to use your MCP server** (as per README)

2. **Ask an AI assistant to analyze a dump file** using WSL paths:
   ```
   "Please analyze the crash dump at /mnt/c/inetpub/wwwroot/uploads/dump_20250925_112751.dmp using symbols from /mnt/d/symbols"
   ```

3. **Watch your MCP server console** - you'll see the conversion notifications when the AI calls `nexus_open_dump_analyze_session`

### Method 3: Direct Stdio Testing

1. **Run the MCP server in stdio mode:**
   ```bash
   dotnet run --project mcp_nexus
   ```

2. **Send JSON-RPC directly** (paste this and press Enter):
   ```json
   {"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"nexus_open_dump_analyze_session","arguments":{"dumpPath":"/mnt/c/inetpub/wwwroot/uploads/test_dump.dmp","symbolsPath":"/mnt/d/symbols"}}}
   ```

3. **See the logs in real-time** on the console

## What to Look For

When the notifications are working, you'll see these patterns in your console:

### ✅ **Successful Conversion Messages:**
- `Converted WSL path '/mnt/c/...' to Windows path 'C:\...'`
- `Converted WSL symbols path '/mnt/d/...' to Windows path 'D:\...'`

### ✅ **No Messages for Non-WSL Paths:**
- If AI provides `C:\windows\path`, no conversion message appears (working as expected)
- If AI provides `/usr/local/bin`, no conversion message appears (working as expected)

### ✅ **Conversion Happens Before File Operations:**
- The conversion messages appear **before** any file existence checks
- This ensures the system works even if the files don't exist yet

## Example Real-World Scenario

```
[INFO] mcp_nexus.Tools.WindbgTool: NexusOpenDump called with dumpPath: /mnt/c/inetpub/wwwroot/uploads/dump_20250925_112751.dmp, symbolsPath: /mnt/d/symbols
[INFO] mcp_nexus.Tools.WindbgTool: Converted WSL path '/mnt/c/inetpub/wwwroot/uploads/dump_20250925_112751.dmp' to Windows path 'C:\inetpub\wwwroot\uploads\dump_20250925_112751.dmp'
[INFO] mcp_nexus.Tools.WindbgTool: Converted WSL symbols path '/mnt/d/symbols' to Windows path 'D:\symbols'
[DEBUG] mcp_nexus.Tools.WindbgTool: Checking if dump file exists: C:\inetpub\wwwroot\uploads\dump_20250925_112751.dmp
[DEBUG] mcp_nexus.Tools.WindbgTool: Validating symbols path: D:\symbols
```

## Troubleshooting

### If you don't see conversion messages:
1. **Check log level** - ensure it's set to `Information` or lower
2. **Verify WSL path format** - must start with `/mnt/[drive_letter]/`
3. **Test with demo paths** - use the exact paths from the test above

### If you see error messages:
- File not found errors are **normal** - the conversion is still working
- The path conversion happens **before** file validation
- Look for the "Converted WSL path" message regardless of file existence

---

## 🎉 **Success Confirmation**

If you see messages like:
```
[INFO] Converted WSL path '/mnt/c/...' to Windows path 'C:\...'
```

**✅ The WSL path conversion notifications are working perfectly!**

Your MCP server is now successfully handling WSL-style paths from AI agents and providing clear visibility into the conversion process.
