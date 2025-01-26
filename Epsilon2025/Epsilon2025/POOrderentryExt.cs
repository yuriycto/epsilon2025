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


            string webHookUrl = path + "/Webhooks/Company/d402156b-27e0-4567-bf2c-c3090b7d0ed0";
            string jsonPayload = JsonConvert.SerializeObject(salesOrder, Formatting.Indented);


            PXLongOperation.StartOperation(Base,
                () =>
                {

                    using (HttpClient client = new HttpClient())
                    {
                        using (StringContent content =
                               new StringContent(jsonPayload, Encoding.UTF8, "application/json"))
                        {
                            try
                            {
                                HttpResponseMessage response = client.PostAsync(webHookUrl, content).Result;

                                // Ensure the request was successful
                                response.EnsureSuccessStatusCode();

                                // Read and display the response body
                                string responseBody = response.Content.ReadAsStringAsync().Result;

                            }
                            catch (AggregateException ex)
                            {
                                // Handle any errors that occurred during the request
                                PXTrace.WriteError(ex);
                            }
                        }
                    }

                });




            return adapter.Get();
        }
    }
}
