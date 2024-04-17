# Chino Kafu Discord Bot - README

## Introduction

Welcome to Chino Kafu, the Discord bot I've crafted using the dotnet framework. As this is my first attempt at creating a bot, there might be a few errors along the way. Don't hesitate to contact me if you encounter any issues. I'll do my best to resolve them.

To get started, you must download the release file then follow my instructions:
+Add your token to `config.json` and 'otherconfig.json`
+Install *pip install google-generativeai* and *pip install pillow* on python 
+Use cmd or shell to run Lavalink.jar (the command must be like: cd (path to your project. Example:*cd F:\Programming\LavaLink*) then type this to your cmd: **java -jar Lavalink.jar**)

## Requirements

- **dotnet >= 7.0 (Required)**
- **java jar >= 17 (Required)**
- **python 3.9 (Required)**
- **Discord Prefix, Token and Osu token (Required)**

## New update
Big update guys :D : Fixing display error of anime on anilist that's releasing.
Added music system (/play, /pause, /resume, /leave ,..)

## Features

Chino Kafu offers plenty of features for your Discord server:

- **Osu Commands**: Use the command `/ohelp` to access a list of osu-related commands, providing detailed information related to the Osu! game.

- **Anilist Commands**: Type `/anilhelp` to receive a list of commands related to Anilist, which offer assistance in exploring anime and manga information without using browser.

- **Automatically Create VoiceChat Channels**: With just a click, you can create a voice chat channel. Click the channel you want to create, and a new channel will appear. The bot will automatically move you to the newly created channel. You can adjust the channel's initialization in `Program.CS` (VoiceChannelHandler).

- **Conversation with Chino-Chan**: Simply just change the allowChannelId on *ChinoConservationChat* and put your gemini token in *GeminiText* and *GeminiVision* then run, Chino will answer you on that channel without any prefix :D. **. Don't worry i have noted in the code so you easily can change it. If you can't, feel free to contact with me via #nupchan0507 on Discord.**

- **Playing music on your server**: The only thing you do is just join a voice channel and then /start (url or name of the music you want to play) then Chino will come your voice channel and start playing your music. For more information, type /musicHelp
## Special Thanks

I want to express my gratitude to ThomasAunvik for creating an API code connection to Anilist, which made it easier for me to create this bot. I have used [his file](https://github.com/ThomasAunvik/AnimeListBot/tree/master/AnimeListBot/Handler/API/Anilist) during the development process. If i didn't find him, creating Anilist search with dotnet wouldn't be possible for a beginner like me.

## API Used
- [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus)
- [OsuNet](https://github.com/Blackcat76iT/OsuNet/tree/29571b5270b52c628a809225ce32c20573b65a3b)
- [Anilist-GraphQL](https://github.com/AniList/ApiV2-GraphQL-Docs)
- [AnilistAPIcode by ThomasAunvik](https://github.com/ThomasAunvik/AnimeListBot/tree/master?fbclid=IwAR0mYkNMSCsnxpXPIj2hAERlldHlDFkRP1X8gxDB4zaHIncZaV5jcFXEAe8)
- [PythonNet](https://github.com/pythonnet/pythonnet)
- [generativeAI](https://github.com/google/generative-ai-docs)
- [LavaLink4Net](https://github.com/angelobreuer/Lavalink4NET)

Thank you for using Chino Kafu! More features are coming soon~.
