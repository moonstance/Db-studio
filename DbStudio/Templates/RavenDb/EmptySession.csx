using System;
using System.Collections.Generic;
using System.Linq;
using DbStudio.Shared;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Client.Documents.Linq;

// Remember to att a using statement for your namespace containing the db entity models

using (var session = RavenHelper.GetSession(SelectedRavenStore)) {
  {
    //var result = session.Query<T>()
    //.Where(x => x.Name.StartsWith("A"))
    //.ToListWithStats(); // To get ravendb query stats

    //return result;
  }
}
