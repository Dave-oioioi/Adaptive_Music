# Reference Notes

Useful references for the MVP design:

- Levlr: media apps and trigger apps are configured separately, which maps well to Adaptive Music's music target and trigger source lists.
- AutoGoose: validates the product idea of automatic music ducking when another app produces sound.
- Wale: open-source Windows per-app volume control and metering. Its useful ideas are session caching, peak polling, and defensive handling of expired audio sessions.
- Windows Core Audio: the MVP uses per-session volume instead of system master volume so trigger audio remains clear.

The first version intentionally treats browsers at process level. Per-tab browser support would need a browser extension or media-session integration later.
