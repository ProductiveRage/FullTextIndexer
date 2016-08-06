using System;
using System.Data.SQLite;
using System.Linq;
using Dapper;
using FullTextIndexer.Common.Lists;
using MarkdownSharp;
using Tester.SourceData;

namespace Tester
{
	public static class PostDataLoader
	{
		public static NonNullImmutableList<Post> GetFromLocalSqliteFile()
		{
			using (var connection = new SQLiteConnection("data source=Blog.sqlite; Version=3;"))
			{
				var tags = connection.Query<MutablePostTagLink>("SELECT PostTags.PostId, Tags.Tag FROM PostTags INNER JOIN Tags ON Tags.Id = PostTags.TagId");

				var markdownTransformer = new Markdown();
				return connection
					.Query<MutablePost>("SELECT Posts.Id, Posts.Title, Posts.Markdown, Posts.PublishedAt, Authors.Name FROM Posts INNER JOIN Authors ON Authors.Id = Posts.AuthorId")
					.Select(post => new Post(
						post.Id,
						new NonBlankTrimmedString(post.Title),
						new NonBlankTrimmedString(markdownTransformer.Transform(post.Markdown)),
						new NonBlankTrimmedString(post.Name),
						post.PublishedAt,
						new NonNullOrEmptyStringList(tags.Where(tag => tag.PostId == post.Id).Select(tag => tag.Tag))
					))
					.ToNonNullImmutableList();
			}
		}

		private class MutablePostTagLink
		{
			public int PostId { get; set; }
			public string Tag { get; set; }
		}

		private class MutablePost
		{
			public int Id { get; set; }
			public string Title { get; set; }
			public string Markdown { get; set; }
			public string Name { get; set; }
			public DateTime PublishedAt { get; set; }
		}
	}
}
