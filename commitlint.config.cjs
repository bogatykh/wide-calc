module.exports = {
  extends: ["@commitlint/config-conventional"],
  rules: {
    // Allow product names and acronyms (WinUI, SDK, .NET, etc.) in subjects.
    "subject-case": [0],
    "body-max-line-length": [0, "always", 100],
    "footer-max-line-length": [0, "always", 100]
  }
};
