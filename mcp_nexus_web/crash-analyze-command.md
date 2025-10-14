### 📝 **TASK: Windows Crash Dump Analysis System**

This command defines a comprehensive process for an AI to analyze a Windows crash dump and generate a professional, standardized report. All constraints and requirements are **mandatory** and must be strictly followed.

***

### 🔒 **MANDATORY CONSTRAINTS**

* **Source Code:** Prefer source code loaded from the actual source code server instead of **reconstructing** it. In any case, please leave a note about where the source is coming from.
* **Dump Analysis:** You are supposed to use the **capabilities** of the Nexus MCP server to **analyze** the dump file.
* **MCP connection:** **DO NOT** use other tools like **curl** to establish the **connection**. The server and tools should be **available**.

***

### 🎯 **OBJECTIVE**

Analyze the crash dump file specified by `[filename]` to determine the root cause. The AI must produce a professional, comprehensive report in **Markdown (MD) format**, adhering to enterprise debugging standards. The report must provide a complete analysis and all necessary information for a developer to solve the issue, including a definitive **Root Cause Analysis (RCA)**. Prioritize depth of investigation to fully isolate the fault and provide actionable insights. The report must be ready for immediate developer action and be as exhaustive as possible.

***

### **USAGE GUIDE**

#### MCP Tools 🛠️

Core debugging tools for **crash dump analysis**.

* **TOOLS**: Use `tools/call` method to execute debugging operations (**open session**, **run commands**, **close session**).
* **RESOURCES**: Use `resources/read` method to access data (**command lists**, **session lists**, **documentation**, **metrics**).
* After opening an analyze session, **WinDBG commands** can be asynchronously executed.
* Command results can be accessed via the **'nexus_read_dump_analyze_command_result' tool** or **'List Commands' resource**.
* Opening a session without executing commands will not have any effect.

##### Available Tools

| Step Title | Tool Name | Action | Input | Output | Note |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Tooling - Open Session** | `nexus_open_dump_analyze_session` | Open the analyze session for the dump file with the tool from Nexus MCP server. | `dumpPath` (string, required), `symbolsPath` (string, optional) | `sessionid` | This **EXACT sessionid IS REQUIRED TO BE USED** for all following commands in the session. |
| **Tooling - Exec Command** | `nexus_enqueue_async_dump_analyze_command` | Use the tool to start asynchronous execution of the WinDBG commands. | `command` (string, required), `sessionId` (string, required) | `commandId` | This **EXACT commandId IS REQUIRED TO BE USED** for the 'nexus_read_dump_analyze_command_result' tool to get the asynchronous result. |
| **Tooling - Close Session** | `nexus_close_dump_analyze_session` | Use the tool to close the analyze session of the dump file after all commands are executed or the session is not needed anymore. | `sessionId` (string, required) | *(null)* | *(null)* |
| **Tooling - Get Command Result** | `nexus_read_dump_analyze_command_result` | Get status and results of a specific async command that was previously queued. | `sessionId` (string, required), `commandId` (string, required) | `command result and status` | Use this tool to retrieve results from commands executed with nexus_enqueue_async_dump_analyze_command |
| **Extensions - Execute** | `nexus_enqueue_async_extension_command` | Execute an extension workflow (e.g., `stack_with_sources`) that orchestrates multiple WinDBG commands. | `sessionId` (string, required), `extensionName` (string, required), `parameters` (object, optional) | `commandId` (prefixed with `ext-`) | Poll results with `nexus_read_dump_analyze_command_result` |

---

#### MCP Resources 📚

Access data and results using the **`resources/read` method** (**NOT `tools/call`**).

* Use the `resources/read` method to access these resources.
* **Resources** provide data access; **tools** provide action execution.

##### Available Resources

| URI | Name | Description | Input | Note/Usage |
| :--- | :--- | :--- | :--- | :--- |
| `workflows` | **Crash Analysis Workflows** | Comprehensive step-by-step analysis workflows for Windows crash dump investigation. | *(null)* | Access via `MCP resources/read` method. |
| `usage` | **Usage** | Essential tool usage information for MCP Nexus server. | *(null)* | Access via `MCP resources/read` method. |
| `sessions` | **List Sessions** | List all debugging sessions with status and activity information. | *(null)* | Use: `sessions` resource (no parameters - returns all sessions). |
| `commands` | **List Commands** | List async commands from all sessions with status and timing information. | *(null)* | Use: `commands` resource (no parameters - returns all commands). |
| `metrics` | **Performance Metrics** | Get comprehensive performance metrics and statistics. | *(null)* | Use: `metrics` resource (no parameters - returns performance data). |
| `circuits` | **Circuit Breaker Status** | Get circuit breaker status and health information. | *(null)* | Use: `circuits` resource (no parameters - returns circuit status). |
| `health` | **System Health** | Get comprehensive system health status. | *(null)* | Use: `health` resource (no parameters - returns health status). |
| `cache` | **Cache Statistics** | Get cache statistics and memory usage information. | *(null)* | Use: `cache` resource (no parameters - returns cache stats). |

***

### 🔧 **REQUIRED ACTIONS & WORKFLOW**

The following steps must be performed sequentially. Ensure all mandatory rules are followed at each stage.

1. **Initialize Analysis:** Open the analyze session for the dump file with the tool from Nexus MCP server `nexus_open_dump_analyze_session`. Feel free to run multiple sessions in parallel if it helps to make the **analysis** faster, the system **resources** allow that and the commands are independent
2. **Source Code Retrieval:**    
    * Set the source server path: `.srcpath "srv*;[workingdir]\source"`
    * Enable source verbosity: `.srcnoisy 3`
    * Enable the source server: `.srcfix+`
    * Execute the extension to resolve sources for the current stack and frames.
    * Use tool: `nexus_enqueue_async_extension_command` with `sessionId` and `extensionName = "stack_with_sources"`.
    * The tool returns a `commandId` (prefixed with `ext-`)
    * Poll results via `nexus_read_dump_analyze_command_result` using the same `sessionId` and returned `commandId`.
    * When completed successfully the sources for the faulting frames should be avalible in the filesystem
    * If source is not found for the current frame, try `lsa [ADDRESS]` where `ADDRESS` is the instruction address.
    * Note: Source files (if found) will be in `[workingdir]/source`.
3. **Comprehensive and in-depth Analysis:**
    * Perform a thorough analysis to pinpoint the **exact root cause**.
    * Gather all helpful information from the dump.
    * For exceptions, collect all necessary data, including the type and the `what()` string.
    * For timeouts, execute WinDbg commands in single, sequential steps.
    * Run extended WinDbg commands to gain a more detailed view of the issue.
    * **Consider checking** the `workflows` resource for **helpful** commands for the specific issue.
4. **Analyze the root cause:**
    * **Summarize** the results from the previous steps.
    * Analyze all the current data in-depth to find the root cause of the crash.
    * Make an internet research for common ways to **analyze** the specific type of crashes.
    * Consider to reiterate and go back and forth between this and the previous step. **Run further needed or more advanced analysis commands to get the full picture.**
5. **File Generation & Verification:**
    * Generate all required files and ensure they strictly follow all mandatory rules, usability, and style guidelines.
    * Read all generated files to confirm they meet all criteria before finalizing the task.
6. **CRITICAL EXIT:** You **MUST** immediately exit/terminate after completing the analysis report. Do not wait for additional input.

***

#### 📄 **ISSUE LOG PAGES**

Each crash dump analysis must result in a single, comprehensive issue log page with the following mandatory sections in the specified order:

1. **Table of Contents:** Generate an automatic Table of Contents at the beginning of the document to improve navigation.
2. **Executive Summary:** A concise summary of the issue and its severity.
3. **System Information:** Key details about the system where the crash occurred.
4. **Root Cause Analysis:** A detailed explanation of the precise root cause.
5. **Faulting Thread Stack Trace:** The full stack trace of the thread that caused the crash.
6. **Source Code at Faulting Position:** The source code snippet where the crash occurred.
7. **Source Code Analysis:** An analysis of the code snippet, explaining why it led to the crash.
8. **WinDbg Command Output:** The complete output of all WinDbg commands performed with the command itself.
9. **Recommended Fixes and Actions:** Concrete suggestions for a solution, including C++ code examples.
10. **Conclusion & Recommendations:** A final verdict and actionable recommendations, presented as a findings card.
11. **Citation & Reference list:** In case external references were used, you **HAVE TO** mention and explain for what purpose you used the information. Do not forget to add **full, clickable URLs (e.g., starting with `http://` or `https://`)** to the external resources which you have access so that the reader of the analysis can follow up on them.

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
        1 line of code
        2 another line of code
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