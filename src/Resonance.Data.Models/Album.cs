using Newtonsoft.Json;
using Resonance.Common;
using System;
using System.Collections.Generic;

namespace Resonance.Data.Models
{
    [JsonObject("album")]
    public class Album : MediaBase
    {
        [JsonProperty("artists")]
        public virtual HashSet<MediaBundle<Artist>> Artists { get; set; }

        [JsonProperty("discs")]
        public int Discs { get; set; }

        [JsonProperty("duration")]
        public TimeSpan Duration { get; set; }

        [JsonProperty("releaseDate")]
        public int ReleaseDate { get; set; }

        [JsonProperty("tracks")]
        public virtual HashSet<MediaBundle<Track>> Tracks { get; set; }

        public static Album FromDynamic(dynamic result)
        {
            var album = new Album
            {
                Id = DynamicExtensions.GetGuidFromDynamic(result.Id),
                CollectionId = DynamicExtensions.GetGuidFromDynamic(result.CollectionId),
                DateAdded = DynamicExtensions.GetDateTimeFromDynamic(result.DateAdded),
                DateModified = result.DateModified == null ? null : DynamicExtensions.GetDateTimeFromDynamic(result.DateModified),
                Name = result.Name
            };

            return album;
        }

        public void AddTrack(MediaBundle<Track> track)
        {
            Discs = Math.Max(Discs, track.Media.DiscNumber);
            Duration = Duration.Add(track.Media.Duration);

            if (Tracks == null)
            {
                Tracks = new HashSet<MediaBundle<Track>>();
            }

            ReleaseDate = Math.Max(ReleaseDate, track.Media.ReleaseDate);

            Tracks.Add(track);

            if (track.Media.Genres == null)
                return;

            if (Genres == null)
            {
                Genres = new HashSet<Genre>(track.Media.Genres);
            }
            else
            {
                foreach (var genre in track.Media.Genres)
                {
                    Genres.Add(genre);
                }
            }
        }
    }
}