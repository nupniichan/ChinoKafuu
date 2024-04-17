/*
 * This file is part of AnimeList Bot
 *
 * AnimeList Bot is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * AnimeList Bot is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with AnimeList Bot.  If not, see <https://www.gnu.org/licenses/>
 */
using ChinoBot.Engine.Anilist.Objects;
using GraphQL.Types.Relay.DataObjects;

namespace AnimeListBot.Handler.Anilist
{
    public interface IAniMedia
    {
        int? id { get; }
        int? idMal { get; }
        AniMediaTitle title { get; }
        AniMediaType? type { get; }
        AniMediaFormat? format { get; }
        AniMediaStatus? status { get; }

        string description { get; }

        AniFuzzyDate startDate { get; }
        AniFuzzyDate endDate { get; }

        int? episodes { get; }
        int? chapters { get; }
        int? volumes { get; }

        AniMediaCoverImage coverImage { get; }
        string siteUrl { get; }

        string bannerImage { get; }

        float? averageScore { get; }

        float? meanScore { get; }
        string? season { get; }

        string? source { get; }

        List<string> genres { get; }

        int? duration { get; }
        AiringSchedule airingSchedule { get; set; }
    }
}
