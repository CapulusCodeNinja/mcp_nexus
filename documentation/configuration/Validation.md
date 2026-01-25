### Validation Configuration

Controls optional dump validation via `dumpchk`.

#### Settings

```json
{
  "WinAiDbg": {
    "Validation": {
      "DumpChkEnabled": false,
      "DumpChkPath": null,
      "DumpChkTimeoutMs": 60000
    }
  }
}
```

- **`DumpChkEnabled`**: Enables/disables dump validation.
- **`DumpChkPath`**: Optional explicit path to `dumpchk.exe`. If `null`, WinAiDbg will attempt to locate it.
- **`DumpChkTimeoutMs`**: Timeout for the validation step in milliseconds.

#### Notes

- Validation is typically used as a fast pre-flight check before starting a full analysis session.
