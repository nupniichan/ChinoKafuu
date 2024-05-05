# Chino Kafu Discord Bot - README

## Introduction

Welcome to Chino Kafu, the Discord bot I've crafted using the .NET 7.0. As this is my first attempt at creating a bot, there might be a few errors along the way. Don't hesitate to contact me if you encounter any issues. I'll do my best to resolve them.

To get started, you must download the release file then follow my instructions:
- Change your token and some config on `config.json` on *../../../ChinoKafuBotDiscord/Configs/config.json* and add your Api key on *../../../Engine/Gemini.py*
- Install *pip install google-generativeai* and *pip install pillow* and *pip install grpcio* on python
- Use cmd or shell to run Lavalink.jar (the command must be like: cd (path to your project. Example:*cd F:\Programming\LavaLink*) then type this to your cmd: **java -jar Lavalink.jar**)

## Requirements

- **dotnet >= 7.0 (Required)**
- **java jar >= 17 (Required)**
- **python 3.9 (Required)**
- **Discord Prefix, Token and Osu token (Required)**

## New update
I uploaded the newest version but there's a lot of thing to install. Im trying to make it "one click install" command because no-one remember all the install command (including me lol) so the full installation guide will be updated later. But if you want to use it instantly, you can contact me via my social media .. I'll help you about that so don't be hestinate for asking me ( ´ ▽ ` )ﾉ

## Features

Chino Kafu offers plenty of features for your Discord server:

- **Osu Commands**: Use the command `/ohelp` to access a list of osu-related commands, providing detailed information related to the Osu! game.

- **Anilist Commands**: Type `/anilhelp` to receive a list of commands related to Anilist, which offer assistance in exploring anime and manga information without using browser.

- **Automatically Create VoiceChat Channels**: With just a click, you can create a voice chat channel. Click the channel you want to create, and a new channel will appear. The bot will automatically move you to the newly created channel. You can adjust the channel's initialization in `Program.CS` (VoiceChannelHandler).

- **Conversation with Chino-Chan**: You can talk with Chino on your specific channel that you set id on the config file. Also, if you're on the voice chat, she will come and talk what she said with you with Japanese's voice (It might take a long time to get response but dont worry im trying to optimized about that)

- **Playing music on your server**: The only thing you do is just join a voice channel and then /start (url or name of the music you want to play) then Chino will come your voice channel and start playing your music. For more information, type /musicHelp
## Special Thanks

I want to express my gratitude to ThomasAunvik for creating an API code connection to Anilist, which made it easier for me to create this bot. I have used [his file](https://github.com/ThomasAunvik/AnimeListBot/tree/master/AnimeListBot/Handler/API/Anilist) during the anilist's api development process. If i didn't find him, creating Anilist search with .NET wouldn't be possible for a beginner like me.

## API Used
- [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus)
- [OsuNet](https://github.com/Blackcat76iT/OsuNet/tree/29571b5270b52c628a809225ce32c20573b65a3b)
- [Anilist-GraphQL](https://github.com/AniList/ApiV2-GraphQL-Docs)
- [AnilistAPIcode by ThomasAunvik](https://github.com/ThomasAunvik/AnimeListBot/tree/master?fbclid=IwAR0mYkNMSCsnxpXPIj2hAERlldHlDFkRP1X8gxDB4zaHIncZaV5jcFXEAe8)
- [PythonNet](https://github.com/pythonnet/pythonnet)
- [generativeAI](https://github.com/google/generative-ai-docs)
- [LavaLink4Net](https://github.com/angelobreuer/Lavalink4NET)
- [Applio](https://github.com/IAHispano/Applio)
- [FFmpeg](https://github.com/FFmpeg/FFmpeg)

Thank you for using Chino Kafu! More features are coming soon~.
