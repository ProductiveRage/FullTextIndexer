using System;
using FullTextIndexer.Common.Lists;

namespace Tester.SourceData
{
	public sealed class Post
	{
		public Post(int id, NonBlankTrimmedString title, NonBlankTrimmedString body, NonBlankTrimmedString author, DateTime publishedAt, NonNullOrEmptyStringList tags)
		{
            Id = id;
			Title = title ?? throw new ArgumentNullException(nameof(title));
			Body = body ?? throw new ArgumentNullException(nameof(body));
			Author = author ?? throw new ArgumentNullException(nameof(author));
			PublishedAt = publishedAt;
			Tags = tags ?? throw new ArgumentNullException(nameof(tags));
		}

		public int Id { get; }
		public NonBlankTrimmedString Title { get; }
		public NonBlankTrimmedString Body { get; }
		public NonBlankTrimmedString Author { get; }
		public DateTime PublishedAt { get; }
		public NonNullOrEmptyStringList Tags{ get; }
	}
}
