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
using GraphQL.Client.Http;
using GraphQL;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Client.Serializer.Newtonsoft;
using System.Net;

namespace AnimeListBot.Handler.Anilist
{
    public class AniMediaQuery
    {
        public static async Task<IAniMedia> SearchMedia(string mediaSearch, AniMediaType mediaType)
        {
            try
            {
                string query;
                if (mediaType == AniMediaType.MANGA)
                {
                    query = searchMangaQuery;
                }
                else
                {
                    query = searchAnimeQuery;
                }

                var mediaRequest = new GraphQLRequest
                {
                    Query = query,
                    Variables = new
                    {
                        search = mediaSearch,
                        type = Enum.GetName(typeof(AniMediaType), mediaType),
                        asHtml = false
                    }
                };

                using var graphQLClient = new GraphQLHttpClient(AnilistConstants.AnilistAPILink, new NewtonsoftJsonSerializer());
                var response = await graphQLClient.SendQueryAsync<AniMediaResponse>(mediaRequest);
                var media = response.Data.Media;
                media.description = media.description.Replace("<br>", "\n");
                return media;
            }
            catch (Exception e)
            {
                Task.Run(() =>
                {
                    Console.WriteLine($"Lỗi không xác định - {e.Message}");
                }).Wait();
                return null;
            }
        }


        public static async Task<IAniMedia> GetMedia(int id, AniMediaType mediaType)
        {
            try
            {
                string query;
                if (mediaType == AniMediaType.MANGA)
                {
                    query = idMangaQuery;
                }
                else
                {
                    query = idAnimeQuery;
                }

                var mediaRequest = new GraphQLRequest
                {
                    Query = query,
                    Variables = new
                    {
                        id,
                        type = Enum.GetName(typeof(AniMediaType), mediaType),
                        asHtml = false
                    }
                };

                using var graphQLClient = new GraphQLHttpClient(AnilistConstants.AnilistAPILink, new NewtonsoftJsonSerializer());
                var response = await graphQLClient.SendQueryAsync<AniMediaResponse>(mediaRequest);
                var media = response.Data.Media;
                media.description = media.description.Replace("<br>", "\n");
                return media;
            }
            catch (Exception e)
            {
                Task.Run(() =>
                {
                    Console.WriteLine($"Lỗi không xác định - {e.Message}");
                }).Wait();
                return null;
            }
        }

        public const string idAnimeQuery = @"
    query ($id: Int, $type: MediaType, $asHtml: Boolean){
        Media(id: $id, type: $type) {
            id
            idMal
            title {
                romaji
                english
                native
            }
            type
            format
            status
            description(asHtml: $asHtml)
            startDate {
                year
                month
                day
            }
            endDate {
                year
                month
                day
            }
            episodes
            chapters
            volumes
            coverImage {
                large
                medium
            }
            bannerImage
            averageScore
            meanScore
            season
            genres
            source
            siteUrl
            duration
            airingSchedule(page: 1, perPage: 1, notYetAired: true) {
              nodes {
                episode
                timeUntilAiring
              }
            }
        }
    }
";

        public const string searchAnimeQuery = @"
    query ($search: String, $type: MediaType, $asHtml: Boolean){
        Media(search: $search, type: $type) {
            id
            idMal
            title {
                romaji
                english
                native
            }
            type
            format
            status
            description(asHtml: $asHtml)
            startDate {
                year
                month
                day
            }
            endDate {
                year
                month
                day
            }
            episodes
            chapters
            volumes
            coverImage {
                large
                medium
            }
            bannerImage
            averageScore
            meanScore
            season
            genres
            source
            siteUrl
            duration
            airingSchedule(page: 1, perPage: 1, notYetAired: true) {
              nodes {
                episode
                timeUntilAiring
              }
            }
        }
    }
";

        public const string idMangaQuery = @"
    query ($id: Int, $type: MediaType, $asHtml: Boolean){
        Media(id: $id, type: $type) {
            id
            idMal
            title {
                romaji
                english
                native
            }
            type
            format
            status
            description(asHtml: $asHtml)
            startDate {
                year
                month
                day
            }
            endDate {
                year
                month
                day
            }
            chapters
            volumes
            coverImage {
                large
                medium
            }
            bannerImage
            averageScore
            meanScore
            season
            genres
            source
            siteUrl
        }
    }
";

        public const string searchMangaQuery = @"
    query ($search: String, $type: MediaType, $asHtml: Boolean){
        Media(search: $search, type: $type) {
            id
            idMal
            title {
                romaji
                english
                native
            }
            type
            format
            status
            description(asHtml: $asHtml)
            startDate {
                year
                month
                day
            }
            endDate {
                year
                month
                day
            }
            chapters
            volumes
            coverImage {
                large
                medium
            }
            bannerImage
            averageScore
            meanScore
            season
            genres
            source
            siteUrl
        }
    }
";
    }
}
