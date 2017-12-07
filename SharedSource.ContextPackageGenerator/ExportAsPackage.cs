using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Install;
using Sitecore.Install.Items;
using Sitecore.Install.Zip;
using Sitecore.Security.Accounts;
using Sitecore.SecurityModel;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Sites;
using Sitecore.Web.UI.Sheer;

namespace Sitecore.SharedSource.Command.Export
{
    [Serializable]
    public class ExportAsPackage : Shell.Framework.Commands.Command
    {
        public override void Execute(CommandContext context)
        {
            if ((context.Items == null) || (context.Items.Length != 1))
            {
                return;
            }

            //Current User Full name.
            var currentUserName = Context.User.Profile.FullName;

            using (new SecurityDisabler())
            {
                var db = Context.ContentDatabase;

                var document = new PackageProject
                {
                    Metadata =
                    {
                        PackageName = context.Items[0].Name,
                        Author = currentUserName
                    }
                };

                var source = new ExplicitItemSource
                {
                    Name = context.Items[0].Name
                };

                var items = new List<Item>
                {
                    //Self Item
                    db.Items.Database.GetItem(context.Items[0].Paths.Path)
                };

                //decorate item name with # otherwise query will break because of special character.
                var paths = StringUtil.Split(context.Items[0].Paths.Path, '/', true)
                    .Where(p => p != null & p != string.Empty)
                    .Select(p => "#" + p + "#")
                    .ToList();

                // current item and child tree
                var allChildQuery = $"/{StringUtil.Join(paths, "/")}//*";
                var children = db.Items.Database.SelectItems(allChildQuery);
                if (children != null && children.Length > 0)
                {
                    //Get children.
                    items.AddRange(children);
                }

                foreach (var item in items)
                {
                    source.Entries.Add(new ItemReference(item.Uri, false).ToString());
                }

                document.Sources.Add(source);
                document.SaveProject = true;

                // Path where the zip file package will be saved
                var filePath = $"{Settings.DataFolder}/packages/{context.Items[0].Name}_{DateTime.Now:dd_MM_yyyy_hh_mm_ss_fffffff}.zip";

                using (new SiteContextSwitcher(SiteContext.GetSite("shell")))
                {
                    using (var writer = new PackageWriter(filePath))
                    {
                        writer.Initialize(Installer.CreateInstallationContext());

                        PackageGenerator.GeneratePackage(document, writer);
                    }
                }

                SheerResponse.Download(filePath);
            }
        }
        public override CommandState QueryState(CommandContext context)
        {
            if ((context.Items == null) || (context.Items.Length != 1))
            {
                return CommandState.Hidden;
            }

            return base.QueryState(context);
        }
    }


}
