namespace ElementTemplates.Package
{
	using System.IO;

	using Skyline.DataMiner.Utils.SecureCoding.SecureIO;

	internal static class AssistantToolInstaller
	{
		internal static void Install(string setupDirectory, string commonApplicationData)
		{
			var names = new[]
			{
				"element-alarm-template-configuration.md",
				"element-trend-template-configuration.md",
			};
			var sources = new SecurePath[names.Length];
			for (var i = 0; i < names.Length; i++)
			{
				sources[i] = SecurePath.ConstructSecurePath(setupDirectory, "Assistant", names[i]);
				if (!File.Exists(sources[i]))
				{
					throw new FileNotFoundException("A required Assistant tool is missing from the package.", names[i]);
				}
			}

			var destination = SecurePath.ConstructSecurePath(commonApplicationData,
				"Skyline Communications", "DataMiner Assistant", "Synced Documents", "Context", "Custom", "Adhoc");
			Directory.CreateDirectory(destination);
			for (var i = 0; i < names.Length; i++)
			{
				File.Copy(sources[i], SecurePath.ConstructSecurePath(destination, names[i]), true);
			}
		}
	}
}
