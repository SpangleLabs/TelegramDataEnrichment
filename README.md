# Telegram Data Enrichment

This is a plugin for the [https://github.com/ZapDragon/DreadBot](Dreadbot) telegram bot.

This is my first C# project, and partially a platform to get more comfortable with the language.


Development notes on the aims for the bot:
- Buttons.
- Posting either text (dreams), images (deer photos), or video (gifs).
- Tag stuff as species, source, keep or not, or as having a tag or not.
- Either allow one tag (done when any is pressed), or multiple (done when done is pressed).
- Batch count, for how many to send at once? Delete as each is done.
- Start an enrichment session, allow multiple enrichment sessions. Allow stopping an enrichment session.
- Multiple data source options? With custom filters.
- - Files from directory (deer photos)
- - Messages in channel (gifs)
- - API endpoint (dailys)
- Data output options? Can have multiple inputs to one output maybe? (or not, and combine later?)
- Might be nice if it could upload to dailys, but maybe not necessary?
- predefined tags for session, or add as you go
- create session, continue session, or end session? List enrichment sessions with progress (percentage, and count remaining)
- randomise or ordered
