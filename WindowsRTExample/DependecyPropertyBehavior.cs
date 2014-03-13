using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using Windows.UI.Interactivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Binding = System.ServiceModel.Channels.Binding;

namespace WindowsRTExample
{
    public class DependecyPropertyBehavior : Behavior<FrameworkElement>
    {
        private PropertyInfo _propertyInfo;

        public static readonly DependencyProperty BindingProperty = DependencyProperty.Register(
            "Binding",
            typeof(object),
            typeof(DependecyPropertyBehavior),
            new PropertyMetadata(null, PropertyChangedCallback)
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
            if (Property == null) return;

            _propertyInfo = AssociatedObject.GetType().GetTypeInfo().GetDeclaredProperty(Property);
            if (_propertyInfo == null) return;
            if (UpdateEvent == null) return;

            var eventInfo = Observable.FromEventPattern<RoutedEventArgs>(AssociatedObject, UpdateEvent);
            if (eventInfo == null) return;
            
            eventInfo.Subscribe(ep => EventFired());
        }

        private static void PropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (DependecyPropertyBehavior)sender;
            PropertyInfo property = behavior.AssociatedObject.GetType().GetTypeInfo().GetDeclaredProperty(behavior.Property);
            if (property == null) return;

            object oldValue = property.GetValue(behavior.AssociatedObject);

            if (oldValue == null && e.NewValue == null) return;
            if (oldValue != null && oldValue.Equals(e.NewValue)) return;

            property.SetValue(behavior.AssociatedObject, e.NewValue);
        }

        private void EventFired()
        {
            Binding = _propertyInfo.GetValue(AssociatedObject, null);
        }
    }
}
