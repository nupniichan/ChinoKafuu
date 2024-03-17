# Chino Kafu Discord Bot - README

## Introduction

Welcome to Chino Kafu, the Discord bot I've crafted using the dotnet framework. As this is my first attempt at creating a bot, there might be a few errors along the way. Don't hesitate to contact me if you encounter any issues. I'll do my best to resolve them.

To get started, you must download the release file and simply add your token to `config.json` and `token.json`, and everything will work perfectly. Don't forget to install *pip install google-generativeai* on python

## Requirements

- **dotnet 7.0**
- **python 3.9**
- **Discord(Required) Osu token**

## New update
I changed conservation engine from characterAI to gemini for better result. Also, I deleted saucenao API and file relate to it. Some bugs are fixed yay :D

## Features

Chino Kafu offers plenty of features for your Discord server:

- **Osu Commands**: Use the command `/ohelp` to access a list of osu-related commands, providing detailed information related to the Osu! game.

- **Anilist Commands**: Type `/anilhelp` to receive a list of commands related to Anilist, which offer assistance in exploring anime and manga information without using browser.

- **Automatically Create VoiceChat Channels**: With just a click, you can create a voice chat channel. Click the channel you want to create, and a new channel will appear. The bot will automatically move you to the newly created channel. You can adjust the channel's initialization in `Program.CS` (VoiceChannelHandler).

- **Conversation with Chino-Chan**: Simply just change the allowChannelId on *ChinoConservationChat* and put your gemini token in *GeminiText* and *GeminiVision* then run, Chino will answer you on that channel without any prefix :D. **. Don't worry i have noted in the code so you easily can change it. If you can't, feel free to contact with me via #nupchan0507 on Discord.**

## Special Thanks

I want to express my gratitude to ThomasAunvik for creating an API code connection to Anilist, which made it easier for me to create this bot. I have used [his file](https://github.com/ThomasAunvik/AnimeListBot/tree/master/AnimeListBot/Handler/API/Anilist) during the development process. If i didn't find him, creating Anilist search with dotnet wouldn't be possible for a beginner like me.

## Libraries Used
- [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus)
- [OsuNet](https://github.com/Blackcat76iT/OsuNet/tree/29571b5270b52c628a809225ce32c20573b65a3b)
- [Anilist-GraphQL](https://github.com/AniList/ApiV2-GraphQL-Docs)
- [AnilistAPIcode by ThomasAunvik](https://github.com/ThomasAunvik/AnimeListBot/tree/master?fbclid=IwAR0mYkNMSCsnxpXPIj2hAERlldHlDFkRP1X8gxDB4zaHIncZaV5jcFXEAe8)
- [PythonNet](https://github.com/pythonnet/pythonnet)
- [generativeAI](https://github.com/google/generative-ai-docs)

Thank you for using Chino Kafu! More features are coming soon~.
