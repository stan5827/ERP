using MassTransit;
using ErpBackend.Events;

namespace ErpBackend.Sagas;

public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderCreated, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => InventoryReserved, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => InventoryReservationFailed, x => x.CorrelateById(context => context.Message.OrderId));

        Initially(
            When(OrderCreated)
                .Then(context =>
                {
                    context.Saga.OrderNumber = context.Message.OrderNumber;
                    context.Saga.CreatedAt = DateTime.UtcNow;
                })
                .TransitionTo(Submitted)
        );

        During(Submitted,
            When(InventoryReserved)
                .Then(context => context.Saga.InventoryReservedAt = context.Message.ReservedAt)
                .TransitionTo(Reserved),
            When(InventoryReservationFailed)
                .Then(context => context.Saga.FailureReason = context.Message.Reason)
                .TransitionTo(Failed)
        );
    }

    public State Submitted { get; private set; } = null!;
    public State Reserved { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    public Event<ISalesOrderCreated> OrderCreated { get; private set; } = null!;
    public Event<IInventoryReserved> InventoryReserved { get; private set; } = null!;
    public Event<IInventoryReservationFailed> InventoryReservationFailed { get; private set; } = null!;
}
