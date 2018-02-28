using Newtonsoft.Json;
using Resonance.Common;
using System;
using System.Collections.Generic;

namespace Resonance.Data.Models
{
    [JsonObject("track")]
    public class Track : MediaBase
    {
        public Track() : base()
        {
        }

        public Track(Album album) : base()
        {
            AlbumId = album.Id;
        }

        [JsonProperty("albumGain")]
        public double? AlbumGain { get; set; }

        [JsonProperty("album")]
        public Guid AlbumId { get; set; }

        [JsonProperty("albumPeak")]
        public double? AlbumPeak { get; set; }

        [JsonProperty("artists")]
        public virtual HashSet<MediaBundle<Artist>> Artists { get; set; }

        [JsonProperty("bitrate")]
        public int Bitrate { get; set; }

        [JsonProperty("channels")]
        public int Channels { get; set; }

        [JsonProperty("comments")]
        public string Comment { get; set; }

        [JsonProperty("contentType")]
        public string ContentType { get; set; }

        [JsonProperty("coverArt")]
        public HashSet<CoverArt> CoverArt { get; set; }

        [JsonProperty("dateCreated")]
        public DateTime DateFileCreated { get; set; }

        [JsonProperty("dateModified")]
        public DateTime DateFileModified { get; set; }

        [JsonProperty("discNumber")]
        public int DiscNumber { get; set; }

        [JsonProperty("duration")]
        public TimeSpan Duration { get; set; }

        [JsonProperty("fileHash")]
        public string FileHash { get; set; }

        [JsonProperty("hashType")]
        public HashType HashType { get; set; }

        [JsonProperty("number")]
        public int Number { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("producedBy")]
        public virtual HashSet<MediaBundle<Artist>> ProducedBy { get; set; }

        [JsonProperty("releaseDate")]
        public int ReleaseDate { get; set; }

        [JsonProperty("sampleRate")]
        public int SampleRate { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("trackGain")]
        public double? TrackGain { get; set; }

        [JsonProperty("trackPeak")]
        public double? TrackPeak { get; set; }

        [JsonProperty("visible")]
        public bool Visible { get; set; }

        [JsonProperty("writtenBy")]
        public virtual HashSet<MediaBundle<Artist>> WrittenBy { get; set; }

        public static void AddExtra(Track track, dynamic result)
        {
            track.AlbumGain = result.AlbumGain;
            track.AlbumPeak = result.AlbumPeak;
            track.Channels = DynamicExtensions.GetIntFromDynamic(result.Channels);
            track.Comment = result.Comment;
            track.SampleRate = DynamicExtensions.GetIntFromDynamic(result.SampleRate);
            track.TrackGain = result.TrackGain;
            track.TrackPeak = result.TrackPeak;
        }

        public static Track FromDynamic(dynamic result)
        {
            var track = new Track
            {
                Id = DynamicExtensions.GetGuidFromDynamic(result.Id),
                AlbumId = DynamicExtensions.GetGuidFromDynamic(result.AlbumId),
                Bitrate = DynamicExtensions.GetIntFromDynamic(result.Bitrate),
                CollectionId = DynamicExtensions.GetGuidFromDynamic(result.CollectionId),
                ContentType = result.ContentType,
                DateAdded = DynamicExtensions.GetDateTimeFromDynamic(result.DateAdded),
                DateFileCreated = DynamicExtensions.GetDateTimeFromDynamic(result.DateFileCreated),
                DateFileModified = DynamicExtensions.GetDateTimeFromDynamic(result.DateFileModified),
                DateModified = result.DateModified == null ? null : DynamicExtensions.GetDateTimeFromDynamic(result.DateModified),
                DiscNumber = DynamicExtensions.GetIntFromDynamic(result.DiscNumber),
                Duration = TimeSpan.FromMilliseconds(result.Duration),
                Name = result.Name,
                Number = DynamicExtensions.GetIntFromDynamic(result.Number),
                Path = result.Path,
                ReleaseDate = DynamicExtensions.GetIntFromDynamic(result.ReleaseDate),
                Size = Convert.ToInt64(result.Size),
                Visible = Convert.ToBoolean(result.Visible)
            };

            return track;
        }

        public void AddArtists(IEnumerable<MediaBundle<Artist>> artists)
        {
            CollectionExtensions.AddValuesToCollection(Artists, artists);
        }

        public void AddGenres(IEnumerable<Genre> genres)
        {
            CollectionExtensions.AddValuesToCollection(Genres, genres);
        }
    }
}