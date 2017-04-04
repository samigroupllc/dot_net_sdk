
Imports System.Collections.Generic
Imports System.Configuration
Imports System.IO
Imports System.Linq
Imports System.ServiceModel
Imports System.Text
Imports System.Threading.Tasks
Imports NabVelocity.Svc
Imports NabVelocity.Txn

Namespace SampleCodeCSharp
    Module Module1
        Sub Main(args As String())
            '#Region "Preparing the Application to Transact"

            '#Region "Setup Clients"

            Dim certification As Boolean = Boolean.Parse(ConfigurationManager.AppSettings("certification"))

            ' setup service information client from service reference generated code
            Dim svcClient = New CWSServiceInformationClient(New BasicHttpsBinding() With {
                .MaxReceivedMessageSize = 20000000
            }, New EndpointAddress("https://api" & (If(certification, ".cert.", ".")) & "nabcommerce.com/2.0.18/SvcInfo"))

            ' setup transaction client from service reference generated code
            Dim txnClient = New CwsTransactionProcessingClient(New BasicHttpsBinding(), New EndpointAddress("https://api" & (If(certification, ".cert.", ".")) & "nabcommerce.com/2.0.18/Txn"))

            Dim applicationProfileId As String = ConfigurationManager.AppSettings("applicationProfileId")
            Dim merchantProfileId As String = ConfigurationManager.AppSettings("merchantProfileId")



            '#End Region

            '#Region "SignOnWithToken"

            Dim identityToken As String = ConfigurationManager.AppSettings("identityToken")

            Dim sessionToken As String = svcClient.SignOnWithToken(identityToken)

            '#End Region

            '#Region "GetServiceInformation"

            Dim serviceInfo As ServiceInformation = svcClient.GetServiceInformation(sessionToken)

            Dim service As BankcardService = serviceInfo.BankcardServices.First()
            '''/ the serviceId represents the payment processor (global, firstdata, chase, etc.)
            Dim serviceId As String = service.ServiceId
            ' if Capture is supported, the service is host capture
            Dim serviceIsHostCapture As Boolean = service.Operations.Capture
            ' if CaptureAll is supprted, the service is terminal capture
            Dim serviceIsTermCapture As Boolean = service.Operations.CaptureAll

            '#End Region

            '#End Region

            '#Region "Transacting"

            If serviceIsHostCapture Then
                '#Region "Host Capture workflow"

                Try
                    '#Region "Verify"

                    Dim verifyRequest = New BankcardTransaction() With {
                        .TenderData = New BankcardTenderData() With {
                            .CardData = New CardData() With {
                                .CardType = TypeCardType.Visa,
                                .PAN = "5100000000000016",
                                .Expire = "1224"
                            },
                            .CardSecurityData = New CardSecurityData() With {
                                .AVSData = New AVSData() With {
                                    .Street = "123 Rain Road",
                                    .City = "Aurora",
                                    .StateProvince = "CO",
                                    .PostalCode = "80080"
                                },
                                .CVData = "383",
                                .CVDataProvided = CVDataProvided.Provided
                            }
                        },
                        .TransactionData = New BankcardTransactionData() With {
                            .Amount = 0D,
                            .EntryMode = NabVelocity.Txn.EntryMode.Keyed,
                            .IndustryType = NabVelocity.Txn.IndustryType.Ecommerce
                        }
                    }

                    Dim verifyResponse = DirectCast(txnClient.Verify(sessionToken, verifyRequest, applicationProfileId, merchantProfileId, serviceId), BankcardTransactionResponse)

                    Console.WriteLine("(Verify) Status: " & verifyResponse.Status & vbCr & vbLf & "CV Result: " & verifyResponse.CVResult & vbCr & vbLf & "AVS Postal Result: " & verifyResponse.AVSResult.PostalCodeResult & vbCr & vbLf)

                    '#End Region

                    '#Region "Authorize"

                    Dim authRequest = New BankcardTransaction() With {
                        .TenderData = New BankcardTenderData() With {
                            .CardData = New CardData() With {
                                .CardType = TypeCardType.Visa,
                                .PAN = "5100000000000016",
                                .Expire = "1224"
                            },
                            .CardSecurityData = New CardSecurityData() With {
                                .AVSData = New AVSData() With {
                                    .Street = "123 Rain Road",
                                    .City = "Aurora",
                                    .StateProvince = "CO",
                                    .PostalCode = "80080"
                                },
                                .CVData = "383",
                                .CVDataProvided = CVDataProvided.Provided
                            }
                        },
                        .TransactionData = New BankcardTransactionData() With {
                            .CurrencyCode = NabVelocity.Txn.TypeISOCurrencyCodeA3.USD,
                            .OrderNumber = "123456",
                            .Amount = 15.12D,
                            .EntryMode = NabVelocity.Txn.EntryMode.Keyed,
                            .IndustryType = NabVelocity.Txn.IndustryType.Ecommerce
                        }
                    }

                    Dim authResponse = DirectCast(txnClient.Authorize(sessionToken, authRequest, applicationProfileId, merchantProfileId, serviceId), BankcardTransactionResponse)

                    Console.WriteLine("(Authorize) Status: " & authResponse.Status & vbCr & vbLf & "Amount: " & authResponse.Amount & vbCr & vbLf & "ApprovalCode: " & authResponse.ApprovalCode & vbCr & vbLf & "TransactionId: " & authResponse.TransactionId & vbCr & vbLf)

                    '#End Region

                    '#Region "Capture"

                    Dim captureDifferenceData = New BankcardCapture() With {
                        .TransactionId = authResponse.TransactionId,
                        .Amount = authResponse.Amount + 1.11D
                    }

                    Dim captureResponse = DirectCast(txnClient.Capture(sessionToken, captureDifferenceData, applicationProfileId, serviceId), BankcardCaptureResponse)

                    Console.WriteLine("(Capture) Status: " & captureResponse.Status & vbCr & vbLf & "Amount: " & captureResponse.TransactionSummaryData.NetTotals.NetAmount & vbCr & vbLf & "TransactionId: " & captureResponse.TransactionId & vbCr & vbLf)

                    '#End Region

                    '#Region "AuthAndCapture"

                    Dim authAndCaptureRequest = New BankcardTransaction() With {
                        .TenderData = New BankcardTenderData() With {
                            .CardData = New CardData() With {
                                .CardType = TypeCardType.Visa,
                                .PAN = "5100000000000016",
                                .Expire = "1224"
                            },
                            .CardSecurityData = New CardSecurityData() With {
                                .AVSData = New AVSData() With {
                                    .Street = "123 Rain Road",
                                    .City = "Aurora",
                                    .StateProvince = "CO",
                                    .PostalCode = "80080"
                                },
                                .CVData = "383",
                                .CVDataProvided = CVDataProvided.Provided
                            }
                        },
                        .TransactionData = New BankcardTransactionData() With {
                            .CurrencyCode = NabVelocity.Txn.TypeISOCurrencyCodeA3.USD,
                            .OrderNumber = "123456",
                            .Amount = 15.12D,
                            .EntryMode = NabVelocity.Txn.EntryMode.Keyed,
                            .IndustryType = NabVelocity.Txn.IndustryType.Ecommerce
                        }
                    }

                    Dim authAndCapResponse = DirectCast(txnClient.AuthorizeAndCapture(sessionToken, authAndCaptureRequest, applicationProfileId, merchantProfileId, serviceId), BankcardTransactionResponse)

                    Console.WriteLine("(AuthAndCapture) Status: " & authAndCapResponse.Status & vbCr & vbLf & "Amount: " & authAndCapResponse.Amount & vbCr & vbLf & "ApprovalCode: " & authAndCapResponse.ApprovalCode & vbCr & vbLf & "TransactionId: " & authAndCapResponse.TransactionId & vbCr & vbLf)

                    '#End Region

                    '#Region "ReturnById"

                    Dim returnByIdRequest = New BankcardReturn() With {
                        .TransactionId = authAndCapResponse.TransactionId,
                        .TransactionDateTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")
                    }

                    Dim returnByIdResponse = DirectCast(txnClient.ReturnById(sessionToken, returnByIdRequest, applicationProfileId, serviceId), BankcardTransactionResponse)

                    Console.WriteLine("(ReturnById) Status: " & returnByIdResponse.Status & vbCr & vbLf & "Amount: " & returnByIdResponse.Amount & vbCr & vbLf & "ApprovalCode: " & returnByIdResponse.ApprovalCode & vbCr & vbLf & "TransactionId: " & returnByIdResponse.TransactionId & vbCr & vbLf)

                    '#End Region

                    '#Region "ReturnUnlinked"

                    Dim returnRequest = New BankcardTransaction() With {
                        .TenderData = New BankcardTenderData() With {
                            .CardData = New CardData() With {
                                .CardType = TypeCardType.Visa,
                                .PAN = "5100000000000016",
                                .Expire = "1224"
                            }
                        },
                        .TransactionData = New BankcardTransactionData() With {
                            .CurrencyCode = NabVelocity.Txn.TypeISOCurrencyCodeA3.USD,
                            .OrderNumber = "123456",
                            .Amount = 15.12D,
                            .EntryMode = NabVelocity.Txn.EntryMode.Keyed,
                            .IndustryType = NabVelocity.Txn.IndustryType.Ecommerce
                        }
                    }

                    Dim returnUnlinkedResponse = DirectCast(txnClient.ReturnUnlinked(sessionToken, returnRequest, applicationProfileId, merchantProfileId, serviceId), BankcardTransactionResponse)

                    Console.WriteLine("(ReturnUnlinked) Status: " & returnUnlinkedResponse.Status & vbCr & vbLf & "Amount: " & returnUnlinkedResponse.Amount & vbCr & vbLf & "ApprovalCode: " & returnUnlinkedResponse.ApprovalCode & vbCr & vbLf & "TransactionId: " & returnUnlinkedResponse.TransactionId & vbCr & vbLf)

                    '#End Region

                    '#Region "Tokenized Transactions"

                    ' build a transaction
                    ' we only need to use a token in the tender data now
                    Dim tokenizedRequest = New BankcardTransaction() With {
                        .TenderData = New BankcardTenderData() With {
                            .PaymentAccountDataToken = verifyResponse.PaymentAccountDataToken
                        },
                        .TransactionData = New BankcardTransactionData() With {
                            .CurrencyCode = NabVelocity.Txn.TypeISOCurrencyCodeA3.USD,
                            .OrderNumber = "123456",
                            .Amount = 15.12D,
                            .EntryMode = NabVelocity.Txn.EntryMode.Keyed,
                            .IndustryType = NabVelocity.Txn.IndustryType.Ecommerce
                        }
                    }

                    authResponse = DirectCast(txnClient.Authorize(sessionToken, tokenizedRequest, applicationProfileId, merchantProfileId, serviceId), BankcardTransactionResponse)
                    authAndCapResponse = DirectCast(txnClient.AuthorizeAndCapture(sessionToken, tokenizedRequest, applicationProfileId, merchantProfileId, serviceId), BankcardTransactionResponse)
                    returnUnlinkedResponse = DirectCast(txnClient.ReturnUnlinked(sessionToken, tokenizedRequest, applicationProfileId, merchantProfileId, serviceId), BankcardTransactionResponse)

                    '#End Region

                    '#Region "Adjust"

                    Dim adjustReq = New Adjust() With {
                        .Amount = 1.11D,
                        .TransactionId = authAndCapResponse.TransactionId
                    }

                    Dim adjustResponse As Response = txnClient.Adjust(sessionToken, adjustReq, applicationProfileId, serviceId)

                    Console.WriteLine("(Adjust) Status: " & adjustResponse.Status & vbCr & vbLf & "StatusMessage: " & adjustResponse.StatusMessage & vbCr & vbLf & "TransactionId: " & adjustResponse.TransactionId & vbCr & vbLf)

                    '#End Region

                    '#Region "Undo"

                    Dim undoRequest = New BankcardUndo() With {
                        .TransactionId = authResponse.TransactionId
                    }

                    Dim undoResponse As Response = txnClient.Undo(sessionToken, undoRequest, applicationProfileId, serviceId)


                    '#End Region
                    Console.WriteLine("(Undo) Status: " & undoResponse.Status & vbCr & vbLf & "StatusMessage: " & undoResponse.StatusMessage & vbCr & vbLf & "TransactionId: " & undoResponse.TransactionId & vbCr & vbLf)
                Catch ex As FaultException(Of NabVelocity.Txn.CWSValidationResultFault)
                    For Each validationError As NabVelocity.Txn.CWSValidationErrorFault In ex.Detail.Errors
                        Console.WriteLine(String.Format("Validatior error: {0} - {1}", validationError.RuleLocationKey, validationError.RuleMessage))
                    Next

                    '#End Region
                End Try
            End If

            If serviceIsTermCapture Then
                '#Region "Term Capture Workflow"

                Try
                    '#Region "Verify"

                    Dim verifyRequest = New BankcardTransaction() With {
                        .TenderData = New BankcardTenderData() With {
                            .CardData = New CardData() With {
                                .CardType = TypeCardType.Visa,
                                .PAN = "4111111111111111",
                                .Expire = "1224"
                            },
                            .CardSecurityData = New CardSecurityData() With {
                                .AVSData = New AVSData() With {
                                    .Street = "123 Rain Road",
                                    .City = "Aurora",
                                    .StateProvince = "CO",
                                    .PostalCode = "80080"
                                },
                                .CVData = "123",
                                .CVDataProvided = CVDataProvided.Provided
                            }
                        },
                        .TransactionData = New BankcardTransactionData() With {
                            .Amount = 0D,
                            .EntryMode = NabVelocity.Txn.EntryMode.Keyed,
                            .IndustryType = NabVelocity.Txn.IndustryType.Ecommerce
                        }
                    }

                    Dim verifyResponse = DirectCast(txnClient.Verify(sessionToken, verifyRequest, applicationProfileId, merchantProfileId, serviceId), BankcardTransactionResponse)

                    Console.WriteLine("(Verify) Status: " & verifyResponse.Status & vbCr & vbLf & "CV Result: " & verifyResponse.CVResult & vbCr & vbLf & "AVS Postal Result: " & verifyResponse.AVSResult.PostalCodeResult & vbCr & vbLf)

                    '#End Region

                    '#Region "Authorize"

                    Dim authRequest = New BankcardTransaction() With {
                        .TenderData = New BankcardTenderData() With {
                            .CardData = New CardData() With {
                                .CardType = TypeCardType.Visa,
                                .PAN = "4111111111111111",
                                .Expire = "1224"
                            },
                            .CardSecurityData = New CardSecurityData() With {
                                .AVSData = New AVSData() With {
                                    .Street = "123 Rain Road",
                                    .City = "Aurora",
                                    .StateProvince = "CO",
                                    .PostalCode = "80080"
                                },
                                .CVData = "123",
                                .CVDataProvided = CVDataProvided.Provided
                            }
                        },
                        .TransactionData = New BankcardTransactionData() With {
                            .CurrencyCode = NabVelocity.Txn.TypeISOCurrencyCodeA3.USD,
                            .OrderNumber = "123456",
                            .Amount = 15.12D,
                            .EntryMode = NabVelocity.Txn.EntryMode.Keyed,
                            .IndustryType = NabVelocity.Txn.IndustryType.Ecommerce
                        }
                    }

                    Dim authResponse = DirectCast(txnClient.Authorize(sessionToken, authRequest, applicationProfileId, merchantProfileId, serviceId), BankcardTransactionResponse)

                    Console.WriteLine("(Authorize) Status: " & authResponse.Status & vbCr & vbLf & "Amount: " & authResponse.Amount & vbCr & vbLf & "ApprovalCode: " & authResponse.ApprovalCode & vbCr & vbLf & "TransactionId: " & authResponse.TransactionId & vbCr & vbLf)

                    '#End Region

                    '#Region "Capture Selective"

                    Dim captureSelectiveDifferenceData = New BankcardCapture() With {
                        .TransactionId = authResponse.TransactionId,
                        .Amount = authResponse.Amount + 1.11D
                    }

                    Dim captureSelectiveResponses As Response() = txnClient.CaptureSelective(sessionToken, {authResponse.TransactionId}, {captureSelectiveDifferenceData}, applicationProfileId, serviceId)

                    For Each response As NabVelocity.Txn.Response In captureSelectiveResponses
                        If response.Status = Status.Failure Then
                            Console.WriteLine("(Capture Selective) Status: " & response.Status & vbCr & vbLf & "StatusMessage: " & response.StatusMessage & vbCr & vbLf & "TransactionId: " & response.TransactionId & vbCr & vbLf)
                        Else
                            Dim captureResponse = DirectCast(response, BankcardCaptureResponse)

                            Console.WriteLine("(Capture Selective) Status: " & captureResponse.Status & vbCr & vbLf & "Industry: " & captureResponse.IndustryType & vbCr & vbLf & "Sales Count: " & captureResponse.TransactionSummaryData.SaleTotals.Count & vbCr & vbLf & "Sales Amount: " & captureResponse.TransactionSummaryData.SaleTotals.NetAmount & vbCr & vbLf & "Return Count: " & captureResponse.TransactionSummaryData.ReturnTotals.Count & vbCr & vbLf & "Return Amount: " & captureResponse.TransactionSummaryData.ReturnTotals.NetAmount & vbCr & vbLf & "TransactionId: " & captureResponse.TransactionId & vbCr & vbLf)
                        End If
                    Next

                    '#End Region

                    '#Region "ReturnById"

                    Dim returnByIdRequest = New BankcardReturn() With {
                        .TransactionId = authResponse.TransactionId,
                        .TransactionDateTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")
                    }

                    Dim returnByIdResponse = DirectCast(txnClient.ReturnById(sessionToken, returnByIdRequest, applicationProfileId, serviceId), BankcardTransactionResponse)

                    Console.WriteLine("(ReturnById) Status: " & returnByIdResponse.Status & vbCr & vbLf & "Amount: " & returnByIdResponse.Amount & vbCr & vbLf & "ApprovalCode: " & returnByIdResponse.ApprovalCode & vbCr & vbLf & "TransactionId: " & returnByIdResponse.TransactionId & vbCr & vbLf)

                    '#End Region

                    '#Region "ReturnUnlinked"

                    Dim returnRequest = New BankcardTransaction() With {
                        .TenderData = New BankcardTenderData() With {
                            .CardData = New CardData() With {
                                .CardType = TypeCardType.Visa,
                                .PAN = "4111111111111111",
                                .Expire = "1224"
                            }
                        },
                        .TransactionData = New BankcardTransactionData() With {
                            .CurrencyCode = NabVelocity.Txn.TypeISOCurrencyCodeA3.USD,
                            .OrderNumber = "123456",
                            .Amount = 15.12D,
                            .EntryMode = NabVelocity.Txn.EntryMode.Keyed,
                            .IndustryType = NabVelocity.Txn.IndustryType.Ecommerce
                        }
                    }

                    Dim returnUnlinkedResponse = DirectCast(txnClient.ReturnUnlinked(sessionToken, returnRequest, applicationProfileId, merchantProfileId, serviceId), BankcardTransactionResponse)

                    Console.WriteLine("(ReturnUnlinked) Status: " & returnUnlinkedResponse.Status & vbCr & vbLf & "Amount: " & returnUnlinkedResponse.Amount & vbCr & vbLf & "ApprovalCode: " & returnUnlinkedResponse.ApprovalCode & vbCr & vbLf & "TransactionId: " & returnUnlinkedResponse.TransactionId & vbCr & vbLf)

                    '#End Region

                    '#Region "Tokenized Transactions"

                    ' build a transaction
                    ' we only need to use a token in the tender data now
                    Dim tokenizedRequest = New BankcardTransaction() With {
                        .TenderData = New BankcardTenderData() With {
                            .PaymentAccountDataToken = verifyResponse.PaymentAccountDataToken
                        },
                        .TransactionData = New BankcardTransactionData() With {
                            .CurrencyCode = NabVelocity.Txn.TypeISOCurrencyCodeA3.USD,
                            .OrderNumber = "123456",
                            .Amount = 15.12D,
                            .EntryMode = NabVelocity.Txn.EntryMode.Keyed,
                            .IndustryType = NabVelocity.Txn.IndustryType.Ecommerce
                        }
                    }

                    authResponse = DirectCast(txnClient.Authorize(sessionToken, tokenizedRequest, applicationProfileId, merchantProfileId, serviceId), BankcardTransactionResponse)
                    returnUnlinkedResponse = DirectCast(txnClient.ReturnUnlinked(sessionToken, tokenizedRequest, applicationProfileId, merchantProfileId, serviceId), BankcardTransactionResponse)

                    '#End Region

                    '#Region "Adjust"

                    Dim adjustReq = New Adjust() With {
                        .Amount = 1.11D,
                        .TransactionId = authResponse.TransactionId
                    }

                    Dim adjustResponse As Response = txnClient.Adjust(sessionToken, adjustReq, applicationProfileId, serviceId)

                    Console.WriteLine("(Adjust) Status: " & adjustResponse.Status & vbCr & vbLf & "StatusMessage: " & adjustResponse.StatusMessage & vbCr & vbLf & "TransactionId: " & adjustResponse.TransactionId & vbCr & vbLf)

                    '#End Region

                    '#Region "Undo"

                    Dim undoRequest = New BankcardUndo() With {
                        .TransactionId = adjustResponse.TransactionId
                    }

                    Dim undoResponse As Response = txnClient.Undo(sessionToken, undoRequest, applicationProfileId, serviceId)

                    Console.WriteLine("(Undo) Status: " & undoResponse.Status & vbCr & vbLf & "StatusMessage: " & undoResponse.StatusMessage & vbCr & vbLf & "TransactionId: " & undoResponse.TransactionId & vbCr & vbLf)

                    '#End Region

                    '#Region "Capture All"

                    Dim captureAllResponses As Response() = txnClient.CaptureAll(sessionToken, Nothing, Nothing, applicationProfileId, merchantProfileId, serviceId)

                    For Each response As NabVelocity.Txn.Response In captureAllResponses
                        If response.Status = Status.Failure Then
                            Console.WriteLine("(Capture All) Status: " & response.Status & vbCr & vbLf & "StatusMessage: " & response.StatusMessage & vbCr & vbLf & "TransactionId: " & response.TransactionId & vbCr & vbLf)
                        Else
                            Dim captureResponse = DirectCast(response, BankcardCaptureResponse)

                            Console.WriteLine("(Capture All) Status: " & captureResponse.Status & vbCr & vbLf & "Industry: " & captureResponse.IndustryType & vbCr & vbLf & "Sales Count: " & captureResponse.TransactionSummaryData.SaleTotals.Count & vbCr & vbLf & "Sales Amount: " & captureResponse.TransactionSummaryData.SaleTotals.NetAmount & vbCr & vbLf & "Return Count: " & captureResponse.TransactionSummaryData.ReturnTotals.Count & vbCr & vbLf & "Return Amount: " & captureResponse.TransactionSummaryData.ReturnTotals.NetAmount & vbCr & vbLf & "TransactionId: " & captureResponse.TransactionId & vbCr & vbLf)
                        End If

                        '#End Region
                    Next
                Catch ex As FaultException(Of NabVelocity.Txn.CWSValidationResultFault)
                    For Each validationError As NabVelocity.Txn.CWSValidationErrorFault In ex.Detail.Errors
                        Console.WriteLine(String.Format("Validatior error: {0} - {1}", validationError.RuleLocationKey, validationError.RuleMessage))
                    Next

                    '#End Region
                End Try
            End If

            '#End Region
        End Sub
    End Module
End Namespace
