using Automation;
using Cake.Frosting;

Directory.SetCurrentDirectory(Context.Workspaces);
return new CakeHost().UseContext<Context>().Run(["--verbosity", "diagnostic", .. args]);
