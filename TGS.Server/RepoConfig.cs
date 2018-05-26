﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TGS.Interface;

namespace TGS.Server
{
	/// <summary>
	/// Repository specific information for a <see cref="Instance"/>
	/// </summary>
	sealed class RepoConfig : IEquatable<RepoConfig>
	{
		/// <summary>
		/// If this json is setup to support <see cref="TGS.Interface.Components.ITGRepository.GenerateChangelog(out string)"/>
		/// </summary>
		public readonly bool ChangelogSupport;
		/// <summary>
		/// Path to the repository's changelog generator script
		/// </summary>
		public readonly string PathToChangelogPy;
		/// <summary>
		/// Arguments for the changelog gennerator script
		/// </summary>
		public readonly string ChangelogPyArguments;
		/// <summary>
		/// List of python pip dependencies for the changelog generator script
		/// </summary>
		public readonly IList<string> PipDependancies = new List<string>();
		/// <summary>
		/// A list of tasks to run after merge. Failure of a task means bad merge.
		/// All of these tasks are ran in <see cref="TGS.Interface.Components.ITGRepository.RunPostMergeTasks()"/>.
		/// </summary>
		public readonly IList<ITaskHolder> PostMergeTasks = new List<ITaskHolder>();
		/// <summary>
		/// Paths to commit and push to the remote repository
		/// </summary>
		public readonly IList<string> PathsToStage = new List<string>();
		/// <summary>
		/// Directory's whose contents should not be touched when the <see cref="Instance"/> updates
		/// </summary>
		public readonly IList<string> StaticDirectoryPaths = new List<string>();
		/// <summary>
		/// DLL's used by DreamDaemon call()() operations. Must be handled as symlinks to avoid lockups during update operations
		/// </summary>
		public readonly IList<string> DLLPaths = new List<string>();

		/// <summary>
		/// Construct a RepoConfig
		/// </summary>
		/// <param name="path">Path to the config JSON to use</param>
		public RepoConfig(string path)
		{
			if (!File.Exists(path))
				return;
			var rawdata = File.ReadAllText(path);
			var json = JsonConvert.DeserializeObject<IDictionary<string, object>>(rawdata);
			try
			{
				var details = ((JObject)json["changelog"]).ToObject<IDictionary<string, object>>();
				PathToChangelogPy = (string)details["script"];
				ChangelogPyArguments = (string)details["arguments"];
				ChangelogSupport = true;
				try
				{
					PipDependancies = LoadArray(details["pip_dependancies"]);
				}
				catch { }
			}
			catch
			{
				ChangelogSupport = false;
			}
			try
			{
				PostMergeTasks = GeneratePostMergeTasks(json["post_merge_tasks"]);
			}
			catch { }
			try
			{
				PathsToStage = LoadArray(json["synchronize_paths"]);
			}
			catch { }
			try
			{
				StaticDirectoryPaths = LoadArray(json["static_directories"]);
			}
			catch { }
			try
			{
				DLLPaths = LoadArray(json["dlls"]);
			}
			catch { }
		}

		/// <summary>
		/// Convert an array of <see cref="string"/>s to a <see cref="IList{T}"/> of <see cref="string"/>s
		/// </summary>
		/// <param name="o">The <see cref="string"/> array to convert</param>
		/// <returns></returns>
		private static IList<string> LoadArray(object o)
		{
			var res = new List<string>();
			foreach (var I in ((JArray)o))
				res.Add(I.ToObject<string>());
			return res;
		}

		/// <summary>
		/// Generates a new list of <see cref="TaskHolder"/>s for use.
		/// </summary>
		/// <param name="initialJson">The root token for the list of JSON objects describing tasks.</param>
		/// <returns>A list of <see cref="TaskHolder"/>s read from JSON.</returns>
		private static IList<ITaskHolder> GeneratePostMergeTasks(object initialJson)
		{
			var res = new List<ITaskHolder>();

			foreach (var taskData in (JArray)initialJson)
			{
				try
				{
					var task = new TaskHolder(taskData);
					res.Add(task);
				}
				catch
				{
				}
			}

			return res;
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			return Equals(obj as RepoConfig);
		}

		/// <summary>
		/// Check if two <see cref="string"/> <see cref="IList{T}"/>s have the same contents
		/// </summary>
		/// <param name="A">The first <see cref="string"/> <see cref="IList{T}"/></param>
		/// <param name="B">The second <see cref="string"/> <see cref="IList{T}"/></param>
		/// <returns><see langword="true"/> if the <see cref="string"/> <see cref="IList{T}"/>s match, <see langword="false"/> otherwise</returns>
		private static bool ListEquals(IList<string> A, IList<string> B)
		{
			return A.All(B.Contains) && A.Count == B.Count;
		}

		public bool Equals(RepoConfig other)
		{
			return ChangelogSupport == other.ChangelogSupport
				&& PathToChangelogPy == other.PathToChangelogPy
				&& ChangelogPyArguments == other.ChangelogPyArguments
				&& ListEquals(PipDependancies, other.PipDependancies)
				&& ListEquals(PathsToStage, other.PathsToStage)
				&& ListEquals(StaticDirectoryPaths, other.StaticDirectoryPaths)
				&& ListEquals(DLLPaths, other.DLLPaths);
		}

		public override int GetHashCode()
		{
			var hashCode = 1890628544;
			hashCode = hashCode * -1521134295 + ChangelogSupport.GetHashCode();
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PathToChangelogPy);
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ChangelogPyArguments);
			hashCode = hashCode * -1521134295 + EqualityComparer<IList<string>>.Default.GetHashCode(PipDependancies);
			hashCode = hashCode * -1521134295 + EqualityComparer<IList<string>>.Default.GetHashCode(PathsToStage);
			hashCode = hashCode * -1521134295 + EqualityComparer<IList<string>>.Default.GetHashCode(StaticDirectoryPaths);
			hashCode = hashCode * -1521134295 + EqualityComparer<IList<string>>.Default.GetHashCode(DLLPaths);
			return hashCode;
		}

		public static bool operator ==(RepoConfig config1, RepoConfig config2)
		{
			return EqualityComparer<RepoConfig>.Default.Equals(config1, config2);
		}

		public static bool operator !=(RepoConfig config1, RepoConfig config2)
		{
			return !(config1 == config2);
		}
	}
}
