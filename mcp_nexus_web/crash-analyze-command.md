### 📝 **TASK: Windows Crash Dump Analysis System**

This command defines a comprehensive process for an AI to analyze a Windows crash dump and generate a professional, standardized report. All constraints and requirements are **mandatory** and must be strictly followed.

***

### 🔒 **MANDATORY CONSTRAINTS**

* **Source Code:** Prefer source code loaded from the actual source code server instead of reconstruct it. In any case please leave a note about where the source is coming from.
* **Dump Analysis** You are supposed to use the capabilites of the Nexus MCP server to anaylze the dump file.
* **MCP connection** **DO NOT** use other tools like curl to establish the connction. The server and tools should be avalible

***

### 🎯 **OBJECTIVE**

Analyze the crash dump file specified by `[filename]` to determine the root cause. The AI must produce a professional Markdown (MD) **page** that adheres to enterprise debugging standards, providing a comprehensive analysis and all necessary information for a developer to solve the issue.

***

### 🔧 **REQUIRED PREPARATION STEPS**

The following steps must be performed sequentially. Ensure all mandatory rules are followed at each stage.

**MANDATORY** In case any preparation fails, do not continue with anything further, just output some details about the failure in a MD file as described in the "MANDATORY OUTPUT FORMAT" section and just `exit` instead. No need to follow "ISSUE LOG PAGES" in that special situation but please provide some helpful information to find the underlaying issue.

1.  **Confirm MCP servers:** Confirm MCP servers are available
2.  **List available MCP Tools:** List all the available MCP servers and there tools
3.  **Verify MCP Tools:** Verify that `nexus_open_dump`, `nexus_close_dump` and `nexus_exec_debugger_command_async` is available in your tool list
4.  **Analyze issue** In case the verification fails elaborate what the issue might be as the server is set in the global mcp.json of the user account `droller`. Check the configuration file and test the connection.

### 🔧 **REQUIRED ACTIONS & WORKFLOW**

The following steps must be performed sequentially. Ensure all mandatory rules are followed at each stage.

1.  **Initialize Analysis:** Open the dump file with the tool from Nexus MCP server `mcp_nexus_open_dump`
2.  **Source Code Retrieval:**
    * Set the source server path: `.srcpath srv\*[workingdir]\source`
    * Enable source verbosity: `.srcnoisy 3`
    * Enable the source server: `.srcfix+`
    * Attempt to get the source for the analysis: `lsa .`
    * If source is not found, try `lsa [ADDRESS]` where `ADDRESS` is the instruction address
    * Note: Source files (if found) will be in `[workingdir]/source`
3.  **Comprehensive and in-depth Analysis:**
    * Perform a thorough analysis to pinpoint the **exact root cause**
    * Gather all helpful information from the dump
    * For exceptions, collect all necessary data, including the type and the `what()` string
    * For timeouts, execute WinDbg commands in single, sequential steps
    * Run extended WinDbg commands to gain a more detailed view of the issue
4.  **File Generation & Verification:**
    * Generate all required files and ensure they strictly follow all mandatory rules, usability, and style guidelines
    * Read all generated files to confirm they meet all criteria before finalizing the task
5.  **MANDATORY EXIT:** You MUST immediately exit/terminate after completing the analysis report. Do not wait for additional input. Use `exit()` or equivalent to terminate the session immediately after generating the final report.

***

#### 📄 **ISSUE LOG PAGES**

Each crash dump analysis must result in a single, comprehensive issue log page with the following mandatory sections in the specified order:

1.  **Table of Contents:** Generate an automatic Table of Contents at the beginning of the document to improve navigation.
2.  **Executive Summary:** A concise summary of the issue and its severity.
3.  **System Information:** Key details about the system where the crash occurred.
4.  **Root Cause Analysis:** A detailed explanation of the precise root cause.
5.  **Faulting Thread Stack Trace:** The full stack trace of the thread that caused the crash.
6.  **Source Code at Faulting Position:** The source code snippet where the crash occurred.
7.  **Source Code Analysis:** An analysis of the code snippet, explaining why it led to the crash.
8.  **WinDbg Command Output:** The complete output of all WinDbg commands performed.
9.  **Recommended Fixes and Actions:** Concrete suggestions for a solution, including C++ code examples.
10. **Conclusion & Recommendations:** A final verdict and actionable recommendations, presented as a findings card.

***

### 📄 **MANDATORY OUTPUT FORMAT**

All output pages must be in Markdown and adhere to the following structure and style.

* **CRITICAL:** The output must be a single MD file saved to the directory `[outputdir]`.
* The output file must have the same name as the dump file, but with an `.md` extension.
* **Create the output directory:** You MUST create the `[outputdir]` directory if it doesn't exist.
* **Example:** For dump file `crash_data.dmp`, create the file `[outputdir]/crash_data.md`.

***

### 🖥️ **CODE BLOCKS & STYLE**

* **ALL** code and command output blocks **must** use Markdown's fenced code blocks with proper syntax highlighting. Use `cpp` for C++ code, `assembly` for assembly code, and `shell` for WinDbg command output.
* **Line Numbers:** Line numbers **must** be manually added to all code blocks, starting from `1` for each block.
    * The numbers should be formatted as part of the content, allowing the user to copy and paste the code without them.
    * **Example format:**
        ```markdown
        1   line of code
        2   another line of code
        ```
* **Font:** Use a monospace font for all code and command content.
* **Stack Traces:** Each frame in a stack trace **must** be on its own numbered line.
* **Spacing:** **DO NOT** use unnecessary spaces between lines within code blocks.

***

### 🎨 **MARKDOWN PAGE STYLE GUIDE**

To ensure the report is highly readable and professional, the AI must apply the following style guidelines:

* **Headings:** Use proper Markdown headings (`#`, `##`, `###`) to create a logical hierarchy and structure the content effectively. Use a single `#` for the main title of the document.
* **Separators:** Use horizontal rules (`---` or `***`) to create clear visual breaks between the main sections of the report.
* **Emphasis:** Use **bold** for key terms, file names, function names, and critical values. Use *italics* for less critical emphasis or to highlight observations.
* **Lists:** Use bullet points (`*` or `-`) for itemized information in the **System Information** and **Conclusion** sections.
* **Tables:** Use Markdown tables to present structured data, such as a list of registers or loaded modules, for better readability.
* **Clarity:** Use clear, concise language. Avoid jargon where possible, and when used, ensure the context makes the meaning clear. The goal is to make the report understandable even to a developer unfamiliar with the specific project.