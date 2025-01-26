using Newtonsoft.Json;
using PX.Data;
using PX.Objects.PO;
using RestSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;

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
            //var client = new Acumatica.RESTClient.Client.ApiClient(path, ignoreSslErrors: true);

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
                    UnitPrice = line.CuryUnitCost.GetValueOrDefault(), // Use the unit cost
                    UOM = line.UOM
                });
            }

            var salesOrder = new
            {
                OrderType = "SO", // Sales Order type
                CustomerID = vendor.BAccountID, // Map vendor as customer
                OrderDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                OrderDesc = poOrder.OrderDesc,
                Details = salesOrderLines.ToArray()
            };

            var gr = Base;

            var thr = new Thread(
                () =>
                {
                    var obj = PrepareDocument(gr);

                    string webHookUrl = path + "/Webhooks/Company/d402156b-27e0-4567-bf2c-c3090b7d0ed0";
                    string jsonPayload = JsonConvert.SerializeObject(obj, Formatting.Indented);

                    var resp = SendRequest(gr);

                    if (!string.IsNullOrEmpty(resp.ErrorMessage))
                    {

                    }
                }
            );

            thr.Start();
            thr.Join();

            return adapter.Get();
        }

        [InjectDependency]
        private IHttpClientFactory _httpClientFactory { get; set; }

        private RestResponse SendRequest(POOrderEntry graph)
        {
            var url = "https://localhost/Summit2025/Webhooks/Company/d402156b-27e0-4567-bf2c-c3090b7d0ed0";
            var client = new RestClient(_httpClientFactory.CreateClient());
            var request = new RestRequest(url, Method.Post);
            var document = PrepareDocument(graph);
            string json = JsonConvert.SerializeObject(document, Formatting.Indented);
            request.AddJsonBody(json);
            return client.Execute(request);
        }

        private object PrepareDocument(POOrderEntry graph)
        {
            var document = graph.Document.Current;
            var lines = new List<object>();
            foreach (POLine line in graph.Transactions.Select())
            {
                lines.Add(new
                {
                    line.InventoryID,
                    line.UOM,
                    line.OrderQty,
                    line.CuryUnitCost,
                    line.CuryInfoID
                });
            }

            return new
            {
                OrderType = "SO",
                document.VendorID,
                document.OrderDate,
                document.OrderDesc,
                document.CuryInfoID,
                document.CuryID,
                document.BranchID,
                Lines = lines
            };

        }
    }
}
