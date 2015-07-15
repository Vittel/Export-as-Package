using Sitecore.Data.Items;
using System;
using System.Collections.Generic;
using SC = Sitecore;
namespace Sitecore.SharedSource.Command.Export
{
	[Serializable]
	public class ExportAsPackage : SC.Shell.Framework.Commands.Command
	{
		public override void Execute(SC.Shell.Framework.Commands.CommandContext context)
		{
			if ((context.Items == null) || (context.Items.Length != 1))
				return;

			string currentUserName = Sitecore.Context.User.Profile.FullName; //Current User Full name.

			//Admin Account
			Sitecore.Security.Accounts.User scUser = Sitecore.Security.Accounts.User.FromName("sitecore\\admin", false);
			using (new Sitecore.Security.Accounts.UserSwitcher(scUser))
			{
				Sitecore.Data.Database db = Context.ContentDatabase;
				Sitecore.Install.PackageProject document = new Sitecore.Install.PackageProject();

				document.Metadata.PackageName = context.Items[0].Name;
				document.Metadata.Author = currentUserName;

				Sitecore.Install.Items.ExplicitItemSource source = new Sitecore.Install.Items.ExplicitItemSource();
				source.Name = context.Items[0].Name;


				List<Item> items = new List<Item>();

				items.Add(db.Items.Database.GetItem(context.Items[0].Paths.Path)); //Self Item
				var children = db.Items.Database.SelectItems(context.Items[0].Paths.Path + "//*");
				if (children != null && children.Length > 0)
					items.AddRange(children); //Get children.

				foreach (Sitecore.Data.Items.Item item in items)
				{
					source.Entries.Add(new Sitecore.Install.Items.ItemReference(item.Uri, false).ToString());
				}

				document.Sources.Add(source);
				document.SaveProject = true;
				// Path where the zip file package will be saved
				string filePath = Sitecore.Configuration.Settings.DataFolder + "/packages/" + context.Items[0].Name + "_" + DateTime.Now.ToString("dd_MM_yyyy_hh_mm_ss_fffffff") + ".zip";

				using (Sitecore.Install.Zip.PackageWriter writer = new Sitecore.Install.Zip.PackageWriter(filePath))
				{
					Sitecore.Context.SetActiveSite("shell");

					writer.Initialize(Sitecore.Install.Installer.CreateInstallationContext());

					Sitecore.Install.PackageGenerator.GeneratePackage(document, writer);

					Sitecore.Context.SetActiveSite("website");
				}

				Sitecore.Web.UI.Sheer.SheerResponse.Download(filePath);

			}
		}
		public override Shell.Framework.Commands.CommandState QueryState(Shell.Framework.Commands.CommandContext context)
		{
			if ((context.Items == null) || (context.Items.Length != 1))
				return Shell.Framework.Commands.CommandState.Hidden;
			else
				// if single item selected.
				return base.QueryState(context);
		}
	}


}
