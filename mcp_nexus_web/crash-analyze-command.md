# Windows Crash Dump Analysis System

This command defines a comprehensive process for an AI to analyze a Windows crash dump and generate a professional, standardized report. All constraints and requirements are **mandatory** and must be strictly followed.

---

## 🔒 **MANDATORY CONSTRAINTS**

* **File Operations:** **DO NOT** write, create, or modify any files outside of the `[workingdir]` and its subdirectories.
* **Source Code:** Prefer source code loaded from the actual source code server instead of reconstruct it. In any case please leave a note about where the source is coming from.

---

## 🎯 **OBJECTIVE**

Analyze the crash dump file specified by `[filename]` to determine the root cause. The AI must produce a professional Markdown (MD) **page** that adheres to enterprise debugging standards, providing a comprehensive analysis and all necessary information for a developer to solve the issue.

---

## 🔧 **REQUIRED ACTIONS & WORKFLOW**

The following steps must be performed sequentially. Ensure all mandatory rules are followed at each stage.

### 1. **Initialize Analysis**
Use the Nexus MCP server `nexus_open_dump` command with all options enabled (`all options true`) to open the dump file path. Please use the Windows format instead of WSL format for that command.

**Required Parameters:**
- `dump_file_path`: Convert WSL path to Windows format (e.g., `C:\inetpub\wwwroot\uploads\filename.dmp`)
- `symbol_path`: Set to `srv*C:\inetpub\wwwroot\Source-Files*https://msdl.microsoft.com/download/symbols`
- `source_path`: Set to `srv*C:\inetpub\wwwroot\Source-Files`
- `all_options`: Set to `true`

### 2. **Source Code Retrieval**
* Set the source server path: `.srcpath srv*[workingdir]/Source-Files`
* Enable source verbosity: `.srcnoisy 3`
* Enable the source server: `.srcfix+`
* Attempt to get the source for the analysis: `lsa .`
* If source is not found, try `lsa [ADDRESS]` where `ADDRESS` is the instruction address

### 3. **Comprehensive and in-depth Analysis**
* Perform a thorough analysis to pinpoint the **exact root cause**
* Gather all helpful information from the dump
* For exceptions, collect all necessary data, including the type and the `what()` string
* For timeouts, execute WinDbg commands in single, sequential steps
* Run extended WinDbg commands to gain a more detailed view of the issue

### 4. **File Generation & Verification**
* Generate all required files and ensure they strictly follow all mandatory rules, usability, and style guidelines
* Read all generated files to confirm they meet all criteria before finalizing the task

### 5. **Exit the agent**
**Important** Exit the agent after running all steps

---

## 📄 **MANDATORY OUTPUT FORMAT**

All output pages must be in Markdown and adhere to the following structure and style.

* The output must be a single MD file located in a new directory named `Crash-Analysis` within the `[workingdir]`.
* The output file must have the same name as the dump file, but with an `.md` extension.
* **Example:** For `file.dmp`, the output file should be `[workingdir]/Crash-Analysis/file.md`.

### 📄 **ISSUE LOG PAGES**

Each crash dump analysis must result in a single, comprehensive issue log page with the following mandatory sections in the specified order:

1. **Table of Contents:** Generate an automatic Table of Contents at the beginning of the document to improve navigation.
2. **Executive Summary:** A concise summary of the issue and its severity.
3. **System Information:** Key details about the system where the crash occurred.
4. **Root Cause Analysis:** A detailed explanation of the precise root cause.
5. **Faulting Thread Stack Trace:** The full stack trace of the thread that caused the crash.
6. **Source Code at Faulting Position:** The source code snippet where the crash occurred.
7. **Source Code Analysis:** An analysis of the code snippet, explaining why it led to the crash.
8. **WinDbg Command Output:** The complete output of all WinDbg commands performed.
9. **Recommended Fixes and Actions:** Concrete suggestions for a solution, including C++ code examples.
10. **Conclusion & Recommendations:** A final verdict and actionable recommendations, presented as a findings card.

---

## 🖥️ **CODE BLOCKS & STYLE**

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

---

## 🎨 **MARKDOWN PAGE STYLE GUIDE**

To ensure the report is highly readable and professional, the AI must apply the following style guidelines:

* **Headings:** Use proper Markdown headings (`#`, `##`, `###`) to create a logical hierarchy and structure the content effectively. Use a single `#` for the main title of the document.
* **Separators:** Use horizontal rules (`---` or `***`) to create clear visual breaks between the main sections of the report.
* **Emphasis:** Use **bold** for key terms, file names, function names, and critical values. Use *italics* for less critical emphasis or to highlight observations.
* **Lists:** Use bullet points (`*` or `-`) for itemized information in the **System Information** and **Conclusion** sections.
* **Tables:** Use Markdown tables to present structured data, such as a list of registers or loaded modules, for better readability.
* **Clarity:** Use clear, concise language. Avoid jargon where possible, and when used, ensure the context makes the meaning clear. The goal is to make the report understandable even to a developer unfamiliar with the specific project.

---

## 🔍 **ANALYSIS DEPTH REQUIREMENTS**

### **Exception Analysis**
For exception-based crashes, the analysis must include:
- Exception code and flags
- Exception address and parameters
- Exception context and registers
- Exception chain analysis
- Exception handling status

### **Memory Analysis**
For memory-related issues, include:
- Memory allocation status
- Heap corruption detection
- Stack overflow analysis
- Virtual memory layout
- Memory protection violations

### **Thread Analysis**
For threading issues, include:
- Thread state and priority
- Thread synchronization objects
- Deadlock detection
- Thread stack analysis
- Thread context switches

### **Module Analysis**
For module-related crashes, include:
- Loaded module list
- Module version information
- Module dependencies
- Symbol resolution status
- Module memory layout

---

## 📊 **REPORT QUALITY STANDARDS**

### **Completeness**
- All required sections must be present
- All WinDbg commands must be executed and documented
- Source code must be retrieved and analyzed
- Root cause must be definitively identified

### **Accuracy**
- All addresses and values must be correct
- Stack traces must be complete and accurate
- Exception information must be precise
- Memory analysis must be thorough

### **Clarity**
- Technical concepts must be explained clearly
- Code examples must be relevant and correct
- Recommendations must be actionable
- Severity assessment must be accurate

### **Professionalism**
- Report must follow enterprise standards
- Formatting must be consistent throughout
- Language must be professional and clear
- Documentation must be comprehensive

---

## 📝 **NOTES**

* This system is designed for enterprise-level crash analysis
* All reports must meet professional debugging standards
* Source code retrieval is prioritized over reconstruction
* Analysis depth must be comprehensive and thorough
* Reports must be immediately actionable for developers
