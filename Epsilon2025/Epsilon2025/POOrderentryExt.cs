using Acumatica.RESTClient.Api;
using Acumatica.RESTClient.AuthApi;
using Newtonsoft.Json;
using PX.Data;
using PX.Objects.PO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Epsilon2025
{
    public class POOrderentryExt : PXGraphExtension<POOrderEntry>
    {
        public PXAction<POOrder> Sync;

        [PXButton(CommitChanges = true), PXUIField(DisplayName = "Sync Order", MapEnableRights = PXCacheRights.Select)]
        public virtual IEnumerable sync(PXAdapter adapter)
        {
            //string path = "https://hackathon.acumatica.com/Epsilon";
            string path = "https://localhost/Summit2025";
            var client = new Acumatica.RESTClient.Client.ApiClient(path, ignoreSslErrors: true);

            var poOrder = Base.Document.Current;
            if (poOrder == null)
            {
                throw new PXException("No Purchase Order selected.");
            }

            var vendor = Base.vendor.Current;

            var poLines = Base.Transactions.Select();
            var salesOrderLines = new List<object>();
            foreach (POLine line in poLines)
            {
                salesOrderLines.Add(new
                {
                    InventoryID = line.InventoryID, // Use InventoryID from the PO line
                    Quantity = line.OrderQty.GetValueOrDefault(), // Use the ordered quantity
                    UnitPrice = line.CuryUnitCost.GetValueOrDefault() // Use the unit cost
                });
            }

            var salesOrder = new
            {
                OrderType = "SO", // Sales Order type
                CustomerID = vendor.AcctCD.Trim(), // Map vendor as customer
                OrderDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                //OrderDesc = poOrder.OrderDesc,
                Details = new[]
                {
                    new
                    {
                        InventoryID = "A100", // Replace with actual InventoryID mapping logic
                        Quantity = 1,
                        UnitPrice = 100.00 // Replace with actual pricing logic
                    }
                }
            };

            var orderContent = new StringContent(JsonConvert.SerializeObject(salesOrder), Encoding.UTF8, "application/json");


            List<KeyValuePair<string, string>> paramList = new List<KeyValuePair<string, string>> { };

            //client.Login("admin", "team5carrot");
            client.Login("admin", "123");

            string callingPath = path + "/entity/Default/23.200.001" + "/SalesOrder";

            PXLongOperation.StartOperation(Base, () =>
            {
                var t = client.CallApiAsync(path, HttpMethod.Put, paramList, orderContent, HeaderContentType.Json,
                    HeaderContentType.Json).Result;
                client.Logout();
            }
            );

            //PXLongOperation.WaitCompletion(Base.UID);



            return adapter.Get();
        }
    }
}
