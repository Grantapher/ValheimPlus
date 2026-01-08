Removed optional HTTP self-test (previously gated by VPLUS_SELFTEST) to comply with PR review request to trim test/debug-only code from the plugin.

Why: keeps runtime code surface smaller and avoids test-only behavior in production plugin startup. The change keeps the simplified `HttpHelper` (HttpClient-only) and embedded default config — only the optional runtime self-test was removed.

If desired, re-introduce as a non-startup unit test or developer-only script instead of running on plugin startup.