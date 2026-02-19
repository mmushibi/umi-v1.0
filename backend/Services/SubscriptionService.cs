using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(ApplicationDbContext context, ILogger<SubscriptionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Subscription>> GetSubscriptionsAsync()
        {
            try
            {
                return await _context.Subscriptions
                    .Include(s => s.Plan)
                    .Include(s => s.Pharmacy)
                    .Where(s => s.IsActive)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscriptions");
                throw;
            }
        }

        public async Task<Subscription> GetSubscriptionAsync(string id)
        {
            try
            {
                return await _context.Subscriptions
                    .Include(s => s.Plan)
                    .Include(s => s.Pharmacy)
                    .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription with ID: {Id}", id);
                throw;
            }
        }

        public async Task<Subscription> CreateSubscriptionAsync(CreateSubscriptionRequest request)
        {
            try
            {
                // Validate pharmacy exists
                var pharmacy = await _context.Pharmacies.FindAsync(request.PharmacyId);
                if (pharmacy == null)
                {
                    throw new ArgumentException("Pharmacy not found");
                }

                // Validate plan exists
                var plan = await _context.SubscriptionPlans.FindAsync(request.PlanId);
                if (plan == null || !plan.IsActive)
                {
                    throw new ArgumentException("Subscription plan not found or inactive");
                }

                // Calculate end date based on billing cycle
                var endDate = CalculateEndDate(request.StartDate, request.BillingCycle);

                var subscription = new Subscription
                {
                    PlanId = request.PlanId,
                    PharmacyId = request.PharmacyId,
                    StartDate = request.StartDate,
                    EndDate = endDate,
                    Amount = CalculateAmount(plan.Price, request.BillingCycle),
                    Status = "active",
                    AutoRenew = request.AutoRenew,
                    IsActive = true
                };

                _context.Subscriptions.Add(subscription);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created subscription {SubscriptionId} for pharmacy {PharmacyId} with plan {PlanId}",
                    subscription.Id, request.PharmacyId, request.PlanId);

                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription");
                throw;
            }
        }

        public async Task<Subscription> UpdateSubscriptionAsync(string id, UpdateSubscriptionRequest request)
        {
            try
            {
                var subscription = await _context.Subscriptions.FindAsync(id);
                if (subscription == null)
                {
                    throw new ArgumentException("Subscription not found");
                }

                // Update fields if provided
                if (request.PlanId.HasValue)
                {
                    var plan = await _context.SubscriptionPlans.FindAsync(request.PlanId.Value);
                    if (plan == null || !plan.IsActive)
                    {
                        throw new ArgumentException("Subscription plan not found or inactive");
                    }
                    subscription.PlanId = request.PlanId.Value;
                }

                if (request.StartDate.HasValue)
                {
                    subscription.StartDate = request.StartDate.Value;
                }

                if (request.EndDate.HasValue)
                {
                    subscription.EndDate = request.EndDate.Value;
                }

                if (!string.IsNullOrEmpty(request.BillingCycle))
                {
                    subscription.EndDate = CalculateEndDate(subscription.StartDate, request.BillingCycle);
                }

                if (request.AutoRenew.HasValue)
                {
                    subscription.AutoRenew = request.AutoRenew.Value;
                }

                if (!string.IsNullOrEmpty(request.Status))
                {
                    subscription.Status = request.Status;
                }

                subscription.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated subscription {SubscriptionId}", id);

                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription with ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteSubscriptionAsync(string id)
        {
            try
            {
                var subscription = await _context.Subscriptions.FindAsync(id);
                if (subscription == null)
                {
                    return false;
                }

                subscription.IsActive = false;
                subscription.Status = "cancelled";
                subscription.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted subscription {SubscriptionId}", id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subscription with ID: {Id}", id);
                throw;
            }
        }

        public async Task<List<SubscriptionPlan>> GetSubscriptionPlansAsync()
        {
            try
            {
                return await _context.SubscriptionPlans
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Price)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription plans");
                throw;
            }
        }

        public async Task<SubscriptionPlan> GetSubscriptionPlanAsync(int id)
        {
            try
            {
                return await _context.SubscriptionPlans
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription plan with ID: {Id}", id);
                throw;
            }
        }

        public async Task<SubscriptionPlan> CreateSubscriptionPlanAsync(CreateSubscriptionPlanRequest request)
        {
            try
            {
                var plan = new SubscriptionPlan
                {
                    Name = request.Name,
                    Price = request.Price,
                    MaxUsers = request.MaxUsers,
                    MaxBranches = request.MaxBranches,
                    MaxStorageGB = request.MaxStorageGB,
                    Features = request.Features,
                    IsActive = true
                };

                _context.SubscriptionPlans.Add(plan);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created subscription plan {PlanId} with name {Name}", plan.Id, plan.Name);

                return plan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription plan");
                throw;
            }
        }

        public async Task<SubscriptionPlan> UpdateSubscriptionPlanAsync(int id, UpdateSubscriptionPlanRequest request)
        {
            try
            {
                var plan = await _context.SubscriptionPlans.FindAsync(id);
                if (plan == null)
                {
                    throw new ArgumentException("Subscription plan not found");
                }

                // Update fields if provided
                if (!string.IsNullOrEmpty(request.Name))
                {
                    plan.Name = request.Name;
                }

                if (request.Price.HasValue)
                {
                    plan.Price = request.Price.Value;
                }

                if (request.MaxUsers.HasValue)
                {
                    plan.MaxUsers = request.MaxUsers.Value;
                }

                if (request.MaxBranches.HasValue)
                {
                    plan.MaxBranches = request.MaxBranches.Value;
                }

                if (request.MaxStorageGB.HasValue)
                {
                    plan.MaxStorageGB = request.MaxStorageGB.Value;
                }

                if (!string.IsNullOrEmpty(request.Features))
                {
                    plan.Features = request.Features;
                }

                if (request.IsActive.HasValue)
                {
                    plan.IsActive = request.IsActive.Value;
                }

                plan.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated subscription plan {PlanId}", id);

                return plan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription plan with ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteSubscriptionPlanAsync(int id)
        {
            try
            {
                var plan = await _context.SubscriptionPlans.FindAsync(id);
                if (plan == null)
                {
                    return false;
                }

                plan.IsActive = false;
                plan.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted subscription plan {PlanId}", id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subscription plan with ID: {Id}", id);
                throw;
            }
        }

        public async Task<List<Pharmacy>> SearchPharmaciesAsync(string query)
        {
            try
            {
                if (string.IsNullOrEmpty(query) || query.Length < 2)
                {
                    return new List<Pharmacy>();
                }

                return await _context.Pharmacies
                    .Where(p => p.IsActive &&
                               (p.Name.ToLower().Contains(query.ToLower()) ||
                                p.Email.ToLower().Contains(query.ToLower())))
                    .OrderBy(p => p.Name)
                    .Take(10)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching pharmacies with query: {Query}", query);
                throw;
            }
        }

        public async Task<List<Pharmacy>> SearchPharmaciesByPhoneAsync(string query)
        {
            try
            {
                if (string.IsNullOrEmpty(query) || query.Length < 2)
                {
                    return new List<Pharmacy>();
                }

                return await _context.Pharmacies
                    .Where(p => p.IsActive && p.Phone.Contains(query))
                    .OrderBy(p => p.Name)
                    .Take(10)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching pharmacies by phone with query: {Query}", query);
                throw;
            }
        }

        public async Task<List<Payment>> GetSubscriptionPaymentsAsync(string subscriptionId)
        {
            try
            {
                // Query actual payment records from database
                var payments = await _context.Payments
                    .Where(p => p.SubscriptionId == subscriptionId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                return payments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payments for subscription {SubscriptionId}", subscriptionId);
                throw;
            }
        }

        public async Task<Payment> ProcessPaymentAsync(ProcessPaymentRequest request)
        {
            try
            {
                var subscription = await _context.Subscriptions
                    .Include(s => s.Plan)
                    .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId);

                if (subscription == null)
                    throw new ArgumentException("Subscription not found");

                // Create payment record
                var payment = new Payment
                {
                    Id = Guid.NewGuid().ToString(),
                    SubscriptionId = request.SubscriptionId,
                    Amount = request.Amount,
                    Currency = request.Currency ?? "ZMW",
                    PaymentMethod = request.PaymentMethod,
                    Status = "pending",
                    TransactionId = request.TransactionId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = request.UserId,
                    Description = request.Description ?? $"Payment for subscription {subscription.Plan.Name}",
                    Metadata = request.Metadata ?? new Dictionary<string, object>()
                };

                await _context.Payments.AddAsync(payment);
                await _context.SaveChangesAsync();

                // Process payment based on method
                var paymentResult = await ProcessPaymentMethodAsync(payment, request);
                
                // Update payment status
                payment.Status = paymentResult.Success ? "completed" : "failed";
                payment.ProcessedAt = DateTime.UtcNow;
                payment.GatewayResponse = paymentResult.Response;
                payment.FailureReason = paymentResult.FailureReason;
                
                if (paymentResult.Success)
                {
                    // Update subscription if payment is successful
                    await UpdateSubscriptionAfterPaymentAsync(subscription, payment);
                    
                    // Log activity
                    await LogPaymentActivityAsync(payment, "completed");
                }
                else
                {
                    // Log failed payment
                    await LogPaymentActivityAsync(payment, "failed");
                }

                await _context.SaveChangesAsync();
                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for subscription {SubscriptionId}", request.SubscriptionId);
                throw;
            }
        }

        public async Task<PaymentRefund> RefundPaymentAsync(string paymentId, decimal amount, string reason, string userId)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Subscription)
                    .FirstOrDefaultAsync(p => p.Id == paymentId);

                if (payment == null)
                    throw new ArgumentException("Payment not found");

                if (payment.Status != "completed")
                    throw new InvalidOperationException("Only completed payments can be refunded");

                if (amount > payment.Amount)
                    throw new ArgumentException("Refund amount cannot exceed payment amount");

                // Create refund record
                var refund = new PaymentRefund
                {
                    Id = Guid.NewGuid().ToString(),
                    PaymentId = paymentId,
                    Amount = amount,
                    Reason = reason,
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                    Currency = payment.Currency
                };

                await _context.PaymentRefunds.AddAsync(refund);
                await _context.SaveChangesAsync();

                // Process refund through payment gateway
                var refundResult = await ProcessRefundAsync(payment, refund);
                
                refund.Status = refundResult.Success ? "completed" : "failed";
                refund.ProcessedAt = DateTime.UtcNow;
                refund.GatewayResponse = refundResult.Response;
                refund.FailureReason = refundResult.FailureReason;

                if (refundResult.Success)
                {
                    // Update payment status
                    payment.RefundedAmount += amount;
                    if (payment.RefundedAmount >= payment.Amount)
                    {
                        payment.Status = "refunded";
                    }
                    
                    // Log refund activity
                    await LogRefundActivityAsync(refund, "completed");
                }
                else
                {
                    await LogRefundActivityAsync(refund, "failed");
                }

                await _context.SaveChangesAsync();
                return refund;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refunding payment {PaymentId}", paymentId);
                throw;
            }
        }

        public async Task<List<PaymentDto>> GetPaymentHistoryAsync(string subscriptionId, PaymentHistoryFilterDto filter)
        {
            try
            {
                var query = _context.Payments
                    .Include(p => p.Refunds)
                    .Where(p => p.SubscriptionId == subscriptionId);

                if (filter.Status?.Any() == true)
                {
                    query = query.Where(p => filter.Status.Contains(p.Status));
                }

                if (filter.PaymentMethod?.Any() == true)
                {
                    query = query.Where(p => filter.PaymentMethod.Contains(p.PaymentMethod));
                }

                if (filter.StartDate.HasValue)
                {
                    query = query.Where(p => p.CreatedAt >= filter.StartDate.Value);
                }

                if (filter.EndDate.HasValue)
                {
                    query = query.Where(p => p.CreatedAt <= filter.EndDate.Value);
                }

                var payments = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(p => new PaymentDto
                    {
                        Id = p.Id,
                        SubscriptionId = p.SubscriptionId,
                        Amount = p.Amount,
                        Currency = p.Currency,
                        PaymentMethod = p.PaymentMethod,
                        Status = p.Status,
                        TransactionId = p.TransactionId,
                        Description = p.Description,
                        CreatedAt = p.CreatedAt,
                        ProcessedAt = p.ProcessedAt,
                        RefundedAmount = p.RefundedAmount,
                        Refunds = p.Refunds.Select(r => new PaymentRefundDto
                        {
                            Id = r.Id,
                            PaymentId = r.PaymentId,
                            Amount = r.Amount,
                            Reason = r.Reason,
                            Status = r.Status,
                            CreatedAt = r.CreatedAt,
                            ProcessedAt = r.ProcessedAt
                        }).ToList()
                    })
                    .ToListAsync();

                return payments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment history for subscription {SubscriptionId}", subscriptionId);
                throw;
            }
        }

        public async Task<List<ActivityLog>> GetSubscriptionActivityLogAsync(string subscriptionId)
        {
            try
            {
                return await _context.ActivityLogs
                    .Where(a => a.Description.Contains($"subscription:{subscriptionId}") ||
                               a.Description.Contains($"Subscription {subscriptionId}"))
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(50)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving activity log for subscription {SubscriptionId}", subscriptionId);
                throw;
            }
        }

        public async Task<SubscriptionStats> GetSubscriptionStatsAsync()
        {
            try
            {
                var subscriptions = await _context.Subscriptions
                    .Include(s => s.Plan)
                    .Where(s => s.IsActive)
                    .ToListAsync();

                var today = DateTime.UtcNow.Date;
                var thirtyDaysFromNow = today.AddDays(30);

                var activeSubscriptions = subscriptions.Count(s => s.Status == "active");
                var expiringSoon = subscriptions.Count(s =>
                    s.Status == "active" && s.EndDate <= thirtyDaysFromNow);

                var monthlyRevenue = subscriptions
                    .Where(s => s.Status == "active")
                    .Sum(s => s.Amount);

                return new SubscriptionStats
                {
                    ActiveSubscriptions = activeSubscriptions,
                    ExpiringSoonCount = expiringSoon,
                    MonthlyRevenue = monthlyRevenue,
                    TotalSubscriptions = subscriptions.Count,
                    TotalRevenue = subscriptions.Sum(s => s.Amount)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription stats");
                throw;
            }
        }

        private DateTime CalculateEndDate(DateTime startDate, string billingCycle)
        {
            return billingCycle.ToLower() switch
            {
                "monthly" => startDate.AddMonths(1),
                "quarterly" => startDate.AddMonths(3),
                "annually" => startDate.AddYears(1),
                _ => startDate.AddMonths(1)
            };
        }

        private decimal CalculateAmount(decimal basePrice, string billingCycle)
        {
            return billingCycle.ToLower() switch
            {
                "monthly" => basePrice,
                "quarterly" => basePrice * 3,
                "annually" => basePrice * 12,
                _ => basePrice
            };
        }

        // Payment system helper methods
        private async Task<PaymentResult> ProcessPaymentMethodAsync(Payment payment, ProcessPaymentRequest request)
        {
            try
            {
                // Simulate payment processing based on method
                // In production, this would integrate with actual payment gateways
                await Task.Delay(1000); // Simulate processing time

                var result = new PaymentResult();
                
                switch (request.PaymentMethod.ToLower())
                {
                    case "mobile_money":
                        result = await ProcessMobileMoneyPaymentAsync(payment, request);
                        break;
                    case "bank_transfer":
                        result = await ProcessBankTransferPaymentAsync(payment, request);
                        break;
                    case "credit_card":
                        result = await ProcessCreditCardPaymentAsync(payment, request);
                        break;
                    case "cash":
                        result = await ProcessCashPaymentAsync(payment, request);
                        break;
                    default:
                        result = new PaymentResult
                        {
                            Success = false,
                            FailureReason = "Unsupported payment method"
                        };
                        break;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment method {PaymentMethod}", request.PaymentMethod);
                return new PaymentResult
                {
                    Success = false,
                    FailureReason = "Payment processing failed"
                };
            }
        }

        private async Task<PaymentResult> ProcessMobileMoneyPaymentAsync(Payment payment, ProcessPaymentRequest request)
        {
            // Simulate mobile money processing (MTN, Airtel, Zamtel)
            // In production, integrate with Zambia mobile money APIs
            var phoneNumber = request.Metadata?.GetValueOrDefault("phoneNumber")?.ToString();
            
            if (string.IsNullOrEmpty(phoneNumber) || !IsValidZambianPhoneNumber(phoneNumber))
            {
                return new PaymentResult
                {
                    Success = false,
                    FailureReason = "Invalid Zambian phone number"
                };
            }

            // Simulate API call to mobile money provider
            await Task.Delay(500);
            
            return new PaymentResult
            {
                Success = true,
                Response = $"Mobile money payment processed successfully for {phoneNumber}",
                TransactionId = $"MM{DateTime.UtcNow:yyyyMMddHHmmss}"
            };
        }

        private async Task<PaymentResult> ProcessBankTransferPaymentAsync(Payment payment, ProcessPaymentRequest request)
        {
            // Simulate bank transfer processing
            // In production, integrate with Zambian banking APIs
            var bankAccount = request.Metadata?.GetValueOrDefault("bankAccount")?.ToString();
            var bankName = request.Metadata?.GetValueOrDefault("bankName")?.ToString();
            
            if (string.IsNullOrEmpty(bankAccount))
            {
                return new PaymentResult
                {
                    Success = false,
                    FailureReason = "Bank account number required"
                };
            }

            await Task.Delay(800);
            
            return new PaymentResult
            {
                Success = true,
                Response = $"Bank transfer processed successfully for {bankName} account ****{bankAccount[^4..]}",
                TransactionId = $"BT{DateTime.UtcNow:yyyyMMddHHmmss}"
            };
        }

        private async Task<PaymentResult> ProcessCreditCardPaymentAsync(Payment payment, ProcessPaymentRequest request)
        {
            // Simulate credit card processing
            // In production, integrate with payment gateways like Stripe, PayPal
            var cardNumber = request.Metadata?.GetValueOrDefault("cardNumber")?.ToString();
            var cvv = request.Metadata?.GetValueOrDefault("cvv")?.ToString();
            var expiry = request.Metadata?.GetValueOrDefault("expiry")?.ToString();
            
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 13)
            {
                return new PaymentResult
                {
                    Success = false,
                    FailureReason = "Invalid credit card number"
                };
            }

            await Task.Delay(600);
            
            return new PaymentResult
            {
                Success = true,
                Response = $"Credit card payment processed successfully for card ****{cardNumber[^4..]}",
                TransactionId = $"CC{DateTime.UtcNow:yyyyMMddHHmmss}"
            };
        }

        private async Task<PaymentResult> ProcessCashPaymentAsync(Payment payment, ProcessPaymentRequest request)
        {
            // Cash payments are typically marked as completed immediately
            await Task.Delay(100);
            
            return new PaymentResult
            {
                Success = true,
                Response = "Cash payment recorded successfully",
                TransactionId = $"CS{DateTime.UtcNow:yyyyMMddHHmmss}"
            };
        }

        private async Task<RefundResult> ProcessRefundAsync(Payment payment, PaymentRefund refund)
        {
            try
            {
                // Simulate refund processing based on original payment method
                await Task.Delay(500);
                
                // In production, this would call the appropriate refund API
                // based on the original payment method
                return new RefundResult
                {
                    Success = true,
                    Response = $"Refund processed successfully for payment {payment.Id}",
                    RefundId = $"RF{DateTime.UtcNow:yyyyMMddHHmmss}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund for payment {PaymentId}", payment.Id);
                return new RefundResult
                {
                    Success = false,
                    FailureReason = "Refund processing failed"
                };
            }
        }

        private async Task UpdateSubscriptionAfterPaymentAsync(Subscription subscription, Payment payment)
        {
            try
            {
                // Extend subscription based on payment amount and plan
                var monthsToAdd = (int)(payment.Amount / subscription.Plan.Price);
                
                if (subscription.EndDate < DateTime.UtcNow)
                {
                    // Subscription has expired, start from today
                    subscription.StartDate = DateTime.UtcNow;
                    subscription.EndDate = DateTime.UtcNow.AddMonths(monthsToAdd);
                }
                else
                {
                    // Extend existing subscription
                    subscription.EndDate = subscription.EndDate.AddMonths(monthsToAdd);
                }
                
                subscription.IsActive = true;
                subscription.UpdatedAt = DateTime.UtcNow;
                subscription.LastPaymentAt = DateTime.UtcNow;
                subscription.NextBillingDate = subscription.EndDate.AddDays(-7); // 7 days before expiry
                
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription after payment");
                throw;
            }
        }

        private async Task LogPaymentActivityAsync(Payment payment, string status)
        {
            try
            {
                var activityLog = new ActivityLog
                {
                    UserId = payment.CreatedBy,
                    Action = status == "completed" ? "Payment Completed" : "Payment Failed",
                    Description = $"Payment {payment.Id} for subscription {payment.SubscriptionId} {status}",
                    IpAddress = "127.0.0.1", // Would get from HttpContext in production
                    UserAgent = "System",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging payment activity");
            }
        }

        private async Task LogRefundActivityAsync(PaymentRefund refund, string status)
        {
            try
            {
                var activityLog = new ActivityLog
                {
                    UserId = refund.CreatedBy,
                    Action = status == "completed" ? "Refund Completed" : "Refund Failed",
                    Description = $"Refund {refund.Id} for payment {refund.PaymentId} {status}",
                    IpAddress = "127.0.0.1", // Would get from HttpContext in production
                    UserAgent = "System",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging refund activity");
            }
        }

        private bool IsValidZambianPhoneNumber(string phoneNumber)
        {
            // Basic validation for Zambian phone numbers
            // Format: +26097XXXXXXX, +26096XXXXXXX, 097XXXXXXX, 096XXXXXXX
            var cleanPhone = phoneNumber.Replace(" ", "").Replace("-", "");
            
            return cleanPhone.StartsWith("+260") && cleanPhone.Length == 12 ||
                   cleanPhone.StartsWith("09") && cleanPhone.Length == 10;
        }
    }
}
