using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using NabVelocity.Svc;
using NabVelocity.Txn;

namespace SampleCodeCSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            #region Preparing the Application to Transact

            #region Setup Clients

            bool certification = bool.Parse(ConfigurationManager.AppSettings["certification"]);

            // setup service information client from service reference generated code
            var svcClient = new CWSServiceInformationClient(new BasicHttpsBinding() { MaxReceivedMessageSize = 20000000 },
                new EndpointAddress("https://api" + (certification ? ".cert." : ".") + "nabcommerce.com/2.0.18/SvcInfo"));

            // setup transaction client from service reference generated code
            var txnClient = new CwsTransactionProcessingClient(new BasicHttpsBinding(),
                new EndpointAddress("https://api" + (certification ? ".cert." : ".") + "nabcommerce.com/2.0.18/Txn"));

            string applicationProfileId = ConfigurationManager.AppSettings["applicationProfileId"]; 
            string merchantProfileId = ConfigurationManager.AppSettings["merchantProfileId"]; ;

            #endregion

            #region SignOnWithToken

            string identityToken = ConfigurationManager.AppSettings["identityToken"];

            string sessionToken = svcClient.SignOnWithToken(identityToken);

            #endregion

            #region GetServiceInformation

            ServiceInformation serviceInfo = svcClient.GetServiceInformation(sessionToken);

            BankcardService service = serviceInfo.BankcardServices.First();
            //// the serviceId represents the payment processor (global, firstdata, chase, etc.)
            string serviceId = service.ServiceId;
            // if Capture is supported, the service is host capture
            bool serviceIsHostCapture = service.Operations.Capture;
            // if CaptureAll is supprted, the service is terminal capture
            bool serviceIsTermCapture = service.Operations.CaptureAll;
            
            #endregion

            #endregion

            #region Transacting

            if (serviceIsHostCapture)
            {
                #region Host Capture workflow

                try
                {
                    #region Verify

                    var verifyRequest = new BankcardTransaction()
                    {
                        TenderData = new BankcardTenderData()
                        {
                            CardData = new CardData1()
                            {
                                CardType = TypeCardType.Visa,
                                PAN = "5100000000000016",
                                Expire = "1224",
                            },
                            CardSecurityData = new CardSecurityData1()
                            {
                                AVSData = new AVSData()
                                {
                                    Street = "123 Rain Road",
                                    City = "Aurora",
                                    StateProvince = "CO",
                                    PostalCode = "80080",
                                },
                                CVData = "383",
                                CVDataProvided = CVDataProvided.Provided,
                            },
                        },
                        TransactionData = new BankcardTransactionData()
                        {
                            Amount = 0.00M,
                            EntryMode = NabVelocity.Txn.EntryMode.Keyed,
                            IndustryType = NabVelocity.Txn.IndustryType.Ecommerce,
                        },
                        CustomerData = new TransactionCustomerData()
                        {
                        }
                    };

                    var verifyResponse = (BankcardTransactionResponse)txnClient.Verify(sessionToken, verifyRequest,
                        applicationProfileId, merchantProfileId, serviceId);

                    Console.WriteLine("(Verify) Status: " + verifyResponse.Status + "\r\n"
                                    + "CV Result: " + verifyResponse.CVResult + "\r\n"
                                    + "AVS Postal Result: " + verifyResponse.AVSResult.PostalCodeResult + "\r\n");

                    #endregion

                    #region Authorize

                    var authRequest = new BankcardTransaction()
                    {
                        TenderData = new BankcardTenderData()
                        {
                            CardData = new CardData1()
                            {
                                CardType = TypeCardType.Visa,
                                PAN = "5100000000000016",
                                Expire = "1224",
                            },
                            CardSecurityData = new CardSecurityData1()
                            {
                                AVSData = new AVSData()
                                {
                                    Street = "123 Rain Road",
                                    City = "Aurora",
                                    StateProvince = "CO",
                                    PostalCode = "80080",
                                },
                                CVData = "383",
                                CVDataProvided = CVDataProvided.Provided,
                            },
                        },
                        TransactionData = new BankcardTransactionData()
                        {
                            CurrencyCode = NabVelocity.Txn.TypeISOCurrencyCodeA3.USD,
                            OrderNumber = "123456",
                            Amount = 15.00M,
                            EntryMode = NabVelocity.Txn.EntryMode.Keyed,
                            IndustryType = NabVelocity.Txn.IndustryType.Ecommerce,
                        },
                    };

                    var authResponse = (BankcardTransactionResponse)txnClient.Authorize(sessionToken, authRequest,
                        applicationProfileId, merchantProfileId, serviceId);

                    Console.WriteLine("(Authorize) Status: " + authResponse.Status + "\r\n"
                                    + "Amount: " + authResponse.Amount + "\r\n"
                                    + "ApprovalCode: " + authResponse.ApprovalCode + "\r\n"
                                    + "TransactionId: " + authResponse.TransactionId + "\r\n");

                    #endregion

                    #region Capture

                    var captureDifferenceData = new BankcardCapture()
                    {
                        TransactionId = authResponse.TransactionId,
                        Amount = authResponse.Amount + 1.00M,
                    };

                    var captureResponse = (BankcardCaptureResponse)txnClient.Capture(sessionToken, captureDifferenceData,
                        applicationProfileId, serviceId);

                    Console.WriteLine("(Capture) Status: " + captureResponse.Status + "\r\n"
                                    + "Amount: " + captureResponse.TransactionSummaryData.NetTotals.NetAmount + "\r\n"
                                    + "TransactionId: " + captureResponse.TransactionId + "\r\n");

                    #endregion

                    #region AuthAndCapture

                    var authAndCaptureRequest = new BankcardTransaction()
                    {
                        TenderData = new BankcardTenderData()
                        {
                            CardData = new CardData1()
                            {
                                CardType = TypeCardType.Visa,
                                PAN = "5100000000000016",
                                Expire = "1224",
                            },
                            CardSecurityData = new CardSecurityData1()
                            {
                                AVSData = new AVSData()
                                {
                                    Street = "123 Rain Road",
                                    City = "Aurora",
                                    StateProvince = "CO",
                                    PostalCode = "80080",
                                },
                                CVData = "383",
                                CVDataProvided = CVDataProvided.Provided,
                            },
                        },
                        TransactionData = new BankcardTransactionData()
                        {
                            CurrencyCode = NabVelocity.Txn.TypeISOCurrencyCodeA3.USD,
                            OrderNumber = "123456",
                            Amount = 15.00M,
                            EntryMode = NabVelocity.Txn.EntryMode.Keyed,
                            IndustryType = NabVelocity.Txn.IndustryType.Ecommerce,
                        },
                    };

                    var authAndCapResponse = (BankcardTransactionResponse)txnClient.AuthorizeAndCapture(sessionToken, authAndCaptureRequest,
                        applicationProfileId, merchantProfileId, serviceId);

                    Console.WriteLine("(AuthAndCapture) Status: " + authAndCapResponse.Status + "\r\n"
                                    + "Amount: " + authAndCapResponse.Amount + "\r\n"
                                    + "ApprovalCode: " + authAndCapResponse.ApprovalCode + "\r\n"
                                    + "TransactionId: " + authAndCapResponse.TransactionId + "\r\n");

                    #endregion

                    #region ReturnById

                    var returnByIdRequest = new BankcardReturn()
                    {
                        TransactionId = authAndCapResponse.TransactionId,
                        TransactionDateTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"),
                    };

                    var returnByIdResponse = (BankcardTransactionResponse)txnClient.ReturnById(sessionToken, returnByIdRequest,
                        applicationProfileId, serviceId);

                    Console.WriteLine("(ReturnById) Status: " + returnByIdResponse.Status + "\r\n"
                                    + "Amount: " + returnByIdResponse.Amount + "\r\n"
                                    + "ApprovalCode: " + returnByIdResponse.ApprovalCode + "\r\n"
                                    + "TransactionId: " + returnByIdResponse.TransactionId + "\r\n");

                    #endregion

                    #region ReturnUnlinked

                    var returnRequest = new BankcardTransaction()
                    {
                        TenderData = new BankcardTenderData()
                        {
                            CardData = new CardData1()
                            {
                                CardType = TypeCardType.Visa,
                                PAN = "5100000000000016",
                                Expire = "1224",
                            },
                        },
                        TransactionData = new BankcardTransactionData()
                        {
                            CurrencyCode = NabVelocity.Txn.TypeISOCurrencyCodeA3.USD,
                            OrderNumber = "123456",
                            Amount = 15.00M,
                            EntryMode = NabVelocity.Txn.EntryMode.Keyed,
                            IndustryType = NabVelocity.Txn.IndustryType.Ecommerce,
                        },
                    };

                    var returnUnlinkedResponse = (BankcardTransactionResponse)txnClient.ReturnUnlinked(sessionToken, returnRequest,
                        applicationProfileId, merchantProfileId, serviceId);

                    Console.WriteLine("(ReturnUnlinked) Status: " + returnUnlinkedResponse.Status + "\r\n"
                                    + "Amount: " + returnUnlinkedResponse.Amount + "\r\n"
                                    + "ApprovalCode: " + returnUnlinkedResponse.ApprovalCode + "\r\n"
                                    + "TransactionId: " + returnUnlinkedResponse.TransactionId + "\r\n");

                    #endregion

                    #region Tokenized Transactions

                    // build a transaction
                    var tokenizedRequest = new BankcardTransaction()
                    {
                        TenderData = new BankcardTenderData()
                        {
                            // we only need to use a token in the tender data now
                            PaymentAccountDataToken = verifyResponse.PaymentAccountDataToken
                        },
                        TransactionData = new BankcardTransactionData()
                        {
                            CurrencyCode = NabVelocity.Txn.TypeISOCurrencyCodeA3.USD,
                            OrderNumber = "123456",
                            Amount = 15.00M,
                            EntryMode = NabVelocity.Txn.EntryMode.Keyed,
                            IndustryType = NabVelocity.Txn.IndustryType.Ecommerce,
                        },
                    };

                    authResponse = (BankcardTransactionResponse)txnClient.Authorize(sessionToken, tokenizedRequest,
                        applicationProfileId, merchantProfileId, serviceId);
                    authAndCapResponse = (BankcardTransactionResponse)txnClient.AuthorizeAndCapture(sessionToken, tokenizedRequest,
                        applicationProfileId, merchantProfileId, serviceId);
                    returnUnlinkedResponse = (BankcardTransactionResponse)txnClient.ReturnUnlinked(sessionToken, tokenizedRequest,
                        applicationProfileId, merchantProfileId, serviceId);

                    #endregion

                    #region Adjust

                    var adjustReq = new Adjust()
                    {
                        Amount = 1.00M,
                        TransactionId = authAndCapResponse.TransactionId,
                    };

                    Response adjustResponse = txnClient.Adjust(sessionToken, adjustReq, applicationProfileId, serviceId);

                    Console.WriteLine("(Adjust) Status: " + adjustResponse.Status + "\r\n"
                                    + "StatusMessage: " + adjustResponse.StatusMessage + "\r\n"
                                    + "TransactionId: " + adjustResponse.TransactionId + "\r\n");

                    #endregion

                    #region Undo

                    var undoRequest = new BankcardUndo()
                    {
                        TransactionId = authResponse.TransactionId,
                    };

                    Response undoResponse = txnClient.Undo(sessionToken, undoRequest, applicationProfileId, serviceId);

                    Console.WriteLine("(Undo) Status: " + undoResponse.Status + "\r\n"
                                    + "StatusMessage: " + undoResponse.StatusMessage + "\r\n"
                                    + "TransactionId: " + undoResponse.TransactionId + "\r\n");

                    #endregion
                }
                catch (FaultException<NabVelocity.Txn.CWSValidationResultFault> ex)
                {
                    foreach (var validationError in ex.Detail.Errors)
                    {
                        Console.WriteLine(string.Format("Validatior error: {0} - {1}",
                            validationError.RuleLocationKey, validationError.RuleMessage));
                    }
                }

                #endregion
            }

            if (serviceIsTermCapture)
            {
                #region Term Capture Workflow

                try
                {
                    #region Verify

                    var verifyRequest = new BankcardTransaction()
                    {
                        TenderData = new BankcardTenderData()
                        {
                            CardData = new CardData1()
                            {
                                CardType = TypeCardType.Visa,
                                PAN = "4111111111111111",
                                Expire = "1224",
                            },
                            CardSecurityData = new CardSecurityData1()
                            {
                                AVSData = new AVSData()
                                {
                                    Street = "123 Rain Road",
                                    City = "Aurora",
                                    StateProvince = "CO",
                                    PostalCode = "80080",
                                },
                                CVData = "123",
                                CVDataProvided = CVDataProvided.Provided,
                            },
                        },
                        TransactionData = new BankcardTransactionData()
                        {
                            Amount = 0.00M,
                            EntryMode = NabVelocity.Txn.EntryMode.Keyed,
                            IndustryType = NabVelocity.Txn.IndustryType.Ecommerce,
                        },
                        CustomerData = new TransactionCustomerData()
                        {
                        }
                    };

                    var verifyResponse = (BankcardTransactionResponse)txnClient.Verify(sessionToken, verifyRequest,
                        applicationProfileId, merchantProfileId, serviceId);

                    Console.WriteLine("(Verify) Status: " + verifyResponse.Status + "\r\n"
                                    + "CV Result: " + verifyResponse.CVResult + "\r\n"
                                    + "AVS Postal Result: " + verifyResponse.AVSResult.PostalCodeResult + "\r\n");

                    #endregion

                    #region Authorize

                    var authRequest = new BankcardTransaction()
                    {
                        TenderData = new BankcardTenderData()
                        {
                            CardData = new CardData1()
                            {
                                CardType = TypeCardType.Visa,
                                PAN = "4111111111111111",
                                Expire = "1224",
                            },
                            CardSecurityData = new CardSecurityData1()
                            {
                                AVSData = new AVSData()
                                {
                                    Street = "123 Rain Road",
                                    City = "Aurora",
                                    StateProvince = "CO",
                                    PostalCode = "80080",
                                },
                                CVData = "123",
                                CVDataProvided = CVDataProvided.Provided,
                            },
                        },
                        TransactionData = new BankcardTransactionData()
                        {
                            CurrencyCode = NabVelocity.Txn.TypeISOCurrencyCodeA3.USD,
                            OrderNumber = "123456",
                            Amount = 15.00M,
                            EntryMode = NabVelocity.Txn.EntryMode.Keyed,
                            IndustryType = NabVelocity.Txn.IndustryType.Ecommerce,
                        },
                    };

                    var authResponse = (BankcardTransactionResponse)txnClient.Authorize(sessionToken, authRequest,
                        applicationProfileId, merchantProfileId, serviceId);

                    Console.WriteLine("(Authorize) Status: " + authResponse.Status + "\r\n"
                                    + "Amount: " + authResponse.Amount + "\r\n"
                                    + "ApprovalCode: " + authResponse.ApprovalCode + "\r\n"
                                    + "TransactionId: " + authResponse.TransactionId + "\r\n");

                    #endregion

                    #region Capture Selective

                    var captureSelectiveDifferenceData = new BankcardCapture()
                    {
                        TransactionId = authResponse.TransactionId,
                        Amount = authResponse.Amount + 1.00M,
                    };

                    Response[] captureSelectiveResponses = txnClient.CaptureSelective(sessionToken, new[] { authResponse.TransactionId },
                        new[] { captureSelectiveDifferenceData }, applicationProfileId, serviceId);

                    foreach (var response in captureSelectiveResponses)
                    {
                        if (response.Status == Status.Failure)
                        {
                            Console.WriteLine("(Capture Selective) Status: " + response.Status + "\r\n"
                                            + "StatusMessage: " + response.StatusMessage + "\r\n"
                                            + "TransactionId: " + response.TransactionId + "\r\n");
                        }
                        else
                        {
                            var captureResponse = (BankcardCaptureResponse)response;

                            Console.WriteLine("(Capture Selective) Status: " + captureResponse.Status + "\r\n"
                                + "Industry: " + captureResponse.IndustryType + "\r\n"
                                + "Sales Count: " + captureResponse.TransactionSummaryData.SaleTotals.Count + "\r\n"
                                + "Sales Amount: " + captureResponse.TransactionSummaryData.SaleTotals.NetAmount + "\r\n"
                                + "Return Count: " + captureResponse.TransactionSummaryData.ReturnTotals.Count + "\r\n"
                                + "Return Amount: " + captureResponse.TransactionSummaryData.ReturnTotals.NetAmount + "\r\n"
                                + "TransactionId: " + captureResponse.TransactionId + "\r\n");
                        }
                    }

                    #endregion

                    #region ReturnById

                    var returnByIdRequest = new BankcardReturn()
                    {
                        TransactionId = authResponse.TransactionId,
                        TransactionDateTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"),
                    };

                    var returnByIdResponse = (BankcardTransactionResponse)txnClient.ReturnById(sessionToken, returnByIdRequest,
                        applicationProfileId, serviceId);

                    Console.WriteLine("(ReturnById) Status: " + returnByIdResponse.Status + "\r\n"
                                    + "Amount: " + returnByIdResponse.Amount + "\r\n"
                                    + "ApprovalCode: " + returnByIdResponse.ApprovalCode + "\r\n"
                                    + "TransactionId: " + returnByIdResponse.TransactionId + "\r\n");

                    #endregion

                    #region ReturnUnlinked

                    var returnRequest = new BankcardTransaction()
                    {
                        TenderData = new BankcardTenderData()
                        {
                            CardData = new CardData1()
                            {
                                CardType = TypeCardType.Visa,
                                PAN = "4111111111111111",
                                Expire = "1224",
                            },
                        },
                        TransactionData = new BankcardTransactionData()
                        {
                            CurrencyCode = NabVelocity.Txn.TypeISOCurrencyCodeA3.USD,
                            OrderNumber = "123456",
                            Amount = 15.00M,
                            EntryMode = NabVelocity.Txn.EntryMode.Keyed,
                            IndustryType = NabVelocity.Txn.IndustryType.Ecommerce,
                        },
                    };

                    var returnUnlinkedResponse = (BankcardTransactionResponse)txnClient.ReturnUnlinked(sessionToken, returnRequest,
                        applicationProfileId, merchantProfileId, serviceId);

                    Console.WriteLine("(ReturnUnlinked) Status: " + returnUnlinkedResponse.Status + "\r\n"
                                    + "Amount: " + returnUnlinkedResponse.Amount + "\r\n"
                                    + "ApprovalCode: " + returnUnlinkedResponse.ApprovalCode + "\r\n"
                                    + "TransactionId: " + returnUnlinkedResponse.TransactionId + "\r\n");

                    #endregion

                    #region Tokenized Transactions

                    // build a transaction
                    var tokenizedRequest = new BankcardTransaction()
                    {
                        TenderData = new BankcardTenderData()
                        {
                            // we only need to use a token in the tender data now
                            PaymentAccountDataToken = verifyResponse.PaymentAccountDataToken
                        },
                        TransactionData = new BankcardTransactionData()
                        {
                            CurrencyCode = NabVelocity.Txn.TypeISOCurrencyCodeA3.USD,
                            OrderNumber = "123456",
                            Amount = 15.00M,
                            EntryMode = NabVelocity.Txn.EntryMode.Keyed,
                            IndustryType = NabVelocity.Txn.IndustryType.Ecommerce,
                        },
                    };

                    authResponse = (BankcardTransactionResponse)txnClient.Authorize(sessionToken, tokenizedRequest,
                        applicationProfileId, merchantProfileId, serviceId);
                    returnUnlinkedResponse = (BankcardTransactionResponse)txnClient.ReturnUnlinked(sessionToken, tokenizedRequest,
                        applicationProfileId, merchantProfileId, serviceId);

                    #endregion

                    #region Adjust

                    var adjustReq = new Adjust()
                    {
                        Amount = 1.00M,
                        TransactionId = authResponse.TransactionId,
                    };

                    Response adjustResponse = txnClient.Adjust(sessionToken, adjustReq, applicationProfileId, serviceId);

                    Console.WriteLine("(Adjust) Status: " + adjustResponse.Status + "\r\n"
                                    + "StatusMessage: " + adjustResponse.StatusMessage + "\r\n"
                                    + "TransactionId: " + adjustResponse.TransactionId + "\r\n");

                    #endregion

                    #region Undo

                    var undoRequest = new BankcardUndo()
                    {
                        TransactionId = adjustResponse.TransactionId,
                    };

                    Response undoResponse = txnClient.Undo(sessionToken, undoRequest, applicationProfileId, serviceId);

                    Console.WriteLine("(Undo) Status: " + undoResponse.Status + "\r\n"
                                    + "StatusMessage: " + undoResponse.StatusMessage + "\r\n"
                                    + "TransactionId: " + undoResponse.TransactionId + "\r\n");

                    #endregion

                    #region Capture All

                    Response[] captureAllResponses = txnClient.CaptureAll(sessionToken, null,
                        null, applicationProfileId, merchantProfileId, serviceId);

                    foreach (var response in captureAllResponses)
                    {
                        if (response.Status == Status.Failure)
                        {
                            Console.WriteLine("(Capture All) Status: " + response.Status + "\r\n"
                                            + "StatusMessage: " + response.StatusMessage + "\r\n"
                                            + "TransactionId: " + response.TransactionId + "\r\n");
                        }
                        else
                        {
                            var captureResponse = (BankcardCaptureResponse)response;

                            Console.WriteLine("(Capture All) Status: " + captureResponse.Status + "\r\n"
                                + "Industry: " + captureResponse.IndustryType + "\r\n"
                                + "Sales Count: " + captureResponse.TransactionSummaryData.SaleTotals.Count + "\r\n"
                                + "Sales Amount: " + captureResponse.TransactionSummaryData.SaleTotals.NetAmount + "\r\n"
                                + "Return Count: " + captureResponse.TransactionSummaryData.ReturnTotals.Count + "\r\n"
                                + "Return Amount: " + captureResponse.TransactionSummaryData.ReturnTotals.NetAmount + "\r\n"
                                + "TransactionId: " + captureResponse.TransactionId + "\r\n");
                        }
                    }

                    #endregion
                }
                catch (FaultException<NabVelocity.Txn.CWSValidationResultFault> ex)
                {
                    foreach (var validationError in ex.Detail.Errors)
                    {
                        Console.WriteLine(string.Format("Validatior error: {0} - {1}",
                            validationError.RuleLocationKey, validationError.RuleMessage));
                    }
                }

                #endregion
            }

            #endregion
        }
    }
}
