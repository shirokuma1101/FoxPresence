# Privacy Policy

Last updated: September 8, 2026

FoxPresence reads media playback information only on the supported YouTube, YouTube Music, dアニメストア, U-NEXT, and Netflix pages while the extension is installed and enabled.

## Data handled

FoxPresence handles the current page URL, media title, creator or series name, album or episode information, thumbnail URL, playback state, current playback position, and media duration. This corresponds to Firefox's `browsingActivity`, `websiteContent`, and `websiteActivity` data categories.

## Purpose and transmission

The data is used only to create the Discord Rich Presence requested by the user. The Firefox extension sends it through Firefox Native Messaging to the local FoxPresence Bridge and Tray Application. The Tray Application then sends the presence to the locally running Discord Desktop client through Discord RPC. Discord may process and display this presence according to the user's Discord activity privacy settings and the [Discord Privacy Policy](https://discord.com/privacy).

FoxPresence does not send this data to the FoxPresence developer, an analytics service, an advertising service, or any developer-operated server. It does not sell data or use it for profiling, advertising, or purposes unrelated to Rich Presence.

## Storage and retention

Current media data is kept in memory only while needed for Presence and is cleared after playback stops, the heartbeat expires, Presence is disabled, or FoxPresence exits. Local logs record connection status, supported service identifiers, and errors; they do not record media titles, page URLs, or playback history. Settings stored locally contain the Discord Application ID and FoxPresence preferences.

## Data not accessed

FoxPresence does not access or store browser cookies, service passwords, authentication tokens, Discord user tokens, Bot tokens, personal communications, payment information, or search history outside the supported playback pages.

## User control

Users can stop transmission by pausing playback, disabling Presence from the tray menu, exiting FoxPresence, or uninstalling it. The uninstall script removes FoxPresence settings, Native Messaging registration, startup entries, update entries, and local logs.

## Contact

Questions and privacy requests can be submitted through the [FoxPresence GitHub repository](https://github.com/shirokuma1101/FoxPresence/issues).
