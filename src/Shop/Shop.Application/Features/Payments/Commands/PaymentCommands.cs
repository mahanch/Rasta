using MediatR;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Common;
using Shop.Domain.Entities;
using Shop.Domain.Repositories;

namespace Shop.Application.Features.Payments.Commands;

public record InitiatePaymentCommand(Guid OrderId, string GatewayName = "MockGateway") : IRequest<Result<PaymentInitiateResponseDto>>;
public record PaymentInitiateResponseDto(Guid TransactionId, Guid OrderId, decimal Amount, string TransactionReference, string PaymentUrl);

public class InitiatePaymentCommandHandler : IRequestHandler<InitiatePaymentCommand, Result<PaymentInitiateResponseDto>>
{
    private readonly IOrderRepository _orderRepo;
    private readonly IPaymentRepository _paymentRepo;
    private readonly IUnitOfWork _unitOfWork;

    public InitiatePaymentCommandHandler(IOrderRepository orderRepo, IPaymentRepository paymentRepo, IUnitOfWork unitOfWork)
    {
        _orderRepo = orderRepo;
        _paymentRepo = paymentRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PaymentInitiateResponseDto>> Handle(InitiatePaymentCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepo.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null)
        {
            return Result<PaymentInitiateResponseDto>.Failure(new Error("Payment.OrderNotFound", "Order not found."));
        }

        if (order.Status == OrderStatus.Paid)
        {
            return Result<PaymentInitiateResponseDto>.Failure(new Error("Payment.AlreadyPaid", "Order is already paid."));
        }

        var transaction = PaymentTransaction.Initiate(order.Id, order.FinalAmount, request.GatewayName);
        await _paymentRepo.AddAsync(transaction, cancellationToken);
        await _unitOfWork.CommitChangesAsync(cancellationToken);

        var paymentUrl = $"/api/payments/mock-gateway?orderId={order.Id}&txn={transaction.TransactionReference}";
        return Result<PaymentInitiateResponseDto>.Success(new PaymentInitiateResponseDto(
            transaction.Id,
            order.Id,
            order.FinalAmount,
            transaction.TransactionReference,
            paymentUrl
        ));
    }
}

public record SimulatePaymentSuccessCommand(Guid OrderId, string? TransactionReference = null) : IRequest<Result>;

public class SimulatePaymentSuccessCommandHandler : IRequestHandler<SimulatePaymentSuccessCommand, Result>
{
    private readonly IOrderRepository _orderRepo;
    private readonly IPaymentRepository _paymentRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILicenseClientService _licenseService;

    public SimulatePaymentSuccessCommandHandler(
        IOrderRepository orderRepo,
        IPaymentRepository paymentRepo,
        IUnitOfWork unitOfWork,
        ILicenseClientService licenseService)
    {
        _orderRepo = orderRepo;
        _paymentRepo = paymentRepo;
        _unitOfWork = unitOfWork;
        _licenseService = licenseService;
    }

    public async Task<Result> Handle(SimulatePaymentSuccessCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepo.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null) return Result.Failure(new Error("Payment.OrderNotFound", "Order not found."));

        var refCode = request.TransactionReference ?? $"TXN-OK-{Guid.NewGuid():N}"[..18].ToUpperInvariant();
        order.MarkAsPaid(refCode);

        var txn = await _paymentRepo.GetByOrderIdAsync(order.Id, cancellationToken);
        if (txn != null)
        {
            txn.MarkSuccess(refCode);
        }

        await _unitOfWork.CommitChangesAsync(cancellationToken);

        // Record order usage on License Server (decrement quota / increment count)
        await _licenseService.RecordOrderUsageAsync(cancellationToken);

        return Result.Success();
    }
}
