using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows;
using System.Windows.Interactivity;
using Expression = System.Linq.Expressions.Expression;

namespace DependecyPropertyBehaviorNamesapce
{
    public class DependecyPropertyBehavior : Behavior<DependencyObject>
    {
        private Delegate _handler;
        private EventInfo _eventInfo;
        private PropertyInfo _propertyInfo;

        public static readonly DependencyProperty BindingProperty = DependencyProperty.RegisterAttached(
            "Binding",
            typeof(object),
            typeof(DependecyPropertyBehavior),
            new FrameworkPropertyMetadata { BindsTwoWayByDefault = true }
            );
        
        public object Binding
        {
            get { return GetValue(BindingProperty); }
            set { SetValue(BindingProperty, value); }
        }

        public string Property { get; set; }
        public string UpdateEvent { get; set; }

        protected override void OnAttached()
        {
            Type elementType = AssociatedObject.GetType();

            // Getting property.

            if (Property == null)
            {
                PresentationTraceSources.DependencyPropertySource.TraceData(
                    TraceEventType.Error,
                    1, 
                    "Target property not defined."
                    );
                return;
            }

            _propertyInfo = elementType.GetProperty(Property, BindingFlags.Instance | BindingFlags.Public);

            if (_propertyInfo == null)
            {
                PresentationTraceSources.DependencyPropertySource.TraceData(
                    TraceEventType.Error,
                    2,
                    string.Format("Property \"{0}\" not found.", Property)
                    );
                return;
            }

            // Getting event.

            if (UpdateEvent == null) return;
            _eventInfo = elementType.GetEvent(UpdateEvent);

            if (_eventInfo == null)
            {
                PresentationTraceSources.MarkupSource.TraceData(
                    TraceEventType.Error, 
                    3,
                    string.Format("Event \"{0}\" not found.", UpdateEvent)
                    );
                return;
            }

            _handler = CreateDelegateForEvent(_eventInfo, EventFired);
            _eventInfo.AddEventHandler(AssociatedObject, _handler);
        }

        protected override void OnDetaching()
        {
            if (_eventInfo == null) return;
            if (_handler == null) return;

            _eventInfo.RemoveEventHandler(AssociatedObject, _handler);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property.Name != "Binding") return;
            if (AssociatedObject == null) return;
            if (_propertyInfo == null) return;

            object oldValue = _propertyInfo.GetValue(AssociatedObject, null);
            if (oldValue.Equals(e.NewValue)) return;
            
            _propertyInfo.SetValue(AssociatedObject, e.NewValue, null);

            base.OnPropertyChanged(e);
        }

        private static Delegate CreateDelegateForEvent(EventInfo eventInfo, Action action)
        {
            ParameterExpression[] parameters = 
                eventInfo
                .EventHandlerType
                .GetMethod("Invoke")
                .GetParameters()
                .Select(parameter => Expression.Parameter(parameter.ParameterType))
                .ToArray();

            return Expression.Lambda(
                eventInfo.EventHandlerType,
                Expression.Call(Expression.Constant(action), "Invoke", Type.EmptyTypes),
                parameters
                )
                .Compile();
        }

        private void EventFired()
        {
            if (AssociatedObject == null) return;
            if (_propertyInfo == null) return;

            Binding = _propertyInfo.GetValue(AssociatedObject, null);
        }
    }
}
