namespace ElementTemplates.Package
{
	using System;

	using Skyline.AppInstaller;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.AppPackages;

	/// <summary>Installs the GQI sources and their Assistant data-tool registrations.</summary>
	internal class Script
	{
		/// <summary>Installs default content and registers the packaged Assistant tools.</summary>
		/// <param name="engine">The package installation engine.</param>
		/// <param name="context">The package installation context.</param>
		[AutomationEntryPoint(AutomationEntryPointType.Types.InstallAppPackage)]
		public void Install(IEngine engine, AppInstallContext context)
		{
			try
			{
				var installer = new AppInstaller(Engine.SLNetRaw, context);
				installer.InstallDefaultContent();
				AssistantToolInstaller.Install(installer.GetSetupContentDirectory(),
					Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
				engine.GenerateInformation("Element template sources and Assistant data tools installed.");
			}
			catch (ScriptAbortException)
			{
				throw;
			}
			catch (ScriptForceAbortException)
			{
				throw;
			}
			catch (ScriptTimeoutException)
			{
				throw;
			}
			catch (Exception ex)
			{
				engine.Log("Install|Unexpected exception: " + ex);
				engine.ExitFail("Element template package installation failed.");
			}
		}
	}
}
