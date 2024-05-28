# Chino Kafu Discord Bot - README

## Introduction

Welcome to Chino Kafu, the Discord bot I've crafted using the .NET 7.0. As this is my first attempt at creating a bot, there might be a few errors along the way. Don't hesitate to contact me if you encounter any issues. I'll do my best to resolve them.

## Requirements

- **dotnet >= 7.0 (Required)**
- **java jar >= 17 (Required)**
- **python 3.9 (Required)**
- **Config the file (Required)**

## Installation Guide

### First run
For the first run. You must run run-install.bat on Applio-3.2.0 folder
Then config the file on ChinoKafuRelease\ChinoKafuBotDiscord\Configs\config.json 
After that open ChinoBot.csproj on any ide you like to build the file
When you got the .exe file located on ChinoKafuRelease\ChinoKafuBotDiscord\bin\Debug\net7.0\ChinoBot.exe. Run the *normalrun.bat*. 
The final step is when appilo run successfully, it will direct you to its website. On that web site you have to download chino model on download tab (https://huggingface.co/Timur04129/Chino-Kafuu/resolve/main/chino-kafuu.zip)

### Other runs
Just simply run the normalrun.bat and Chino will work normally

## New update
I uploaded the newest version but there's a lot of thing to install. Im trying to make it "one click install" command because no-one remember all the install command (including me lol) so the full installation guide will be updated later. But if you want to use it instantly, you can contact me via my social media .. I'll help you about that so don't be hestinate for asking me ( ´ ▽ ` )ﾉ

## Features

Chino Kafu offers plenty of features for your Discord server:

- **Osu Commands**: Use the command `/ohelp` to access a list of osu-related commands, providing detailed information related to the Osu! game.

- **Anilist Commands**: Type `/anilhelp` to receive a list of commands related to Anilist, which offer assistance in exploring anime and manga information without using browser.

- **Automatically Create VoiceChat Channels**: With just a click, you can create a voice chat channel. Click the channel you want to create, and a new channel will appear. The bot will automatically move you to the newly created channel. You can adjust the channel's initialization in `Program.CS` (VoiceChannelHandler).

- **Conversation with Chino-Chan**: You can talk with Chino on your specific channel that you set id on the config file. Also, if you're on the voice chat, she will come and talk what she said with you with Japanese's voice (The result take about 20-25 seconds to response you *tested on 2-4 vps no gpu* (depend on hardware you use to host) )

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
