using System;
using System.Collections.Generic;
using System.Linq;
using DbStudio.Shared;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Client.Documents.Linq;
// using MyCompany.Domain.Models; // <-- change this to the namespace where your db entity models reside

// RavenHelper.GetSesssion() uses the store in SelectedRavenStore
// which in turn is shared with other editors connected to the same database.
using (var session = RavenHelper.GetSession(SelectedRavenStore)) {
  {
    //var result = session.Query<T>()       // <-- Change T to your type
    //.Where(x => x.Name.StartsWith("A"))
    //.ToListWithStats();                   // To get ravendb query stats which is displayed in the stats tab in the output panel

    //return result;                        // We need to do a return in order for the script to show the result.
  }
}


// Remember, Ctrl+Shift+C toggles comments on/off for the selected lines.
//           Ctrl+Z for Undo
//           Ctrl+Y for Redo