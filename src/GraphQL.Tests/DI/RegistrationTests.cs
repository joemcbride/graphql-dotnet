using GraphQL.DI;
using GraphQL.Types;

namespace GraphQL.Tests.DI;

public class RegistrationTests : QueryTestBase<RegistrationTests.MySchema>
{
    public override void RegisterServices(IServiceRegister register)
    {
        register.TryRegister(typeof(IClassSingle), typeof(ClassSingle1), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType);
        register.TryRegister(typeof(IClassSingle), typeof(ClassSingle2), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType);

        register.TryRegister<IClassSingle, ClassSingle1>(p => new ClassSingle1(), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType);
        register.TryRegister<IClassSingle, ClassSingle2>(p => new ClassSingle2(), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType);

        register.TryRegister<IClassSingle>(new ClassSingle1(), RegistrationCompareMode.ServiceType);
        register.TryRegister<IClassSingle>(new ClassSingle2(), RegistrationCompareMode.ServiceType);

        //------------

        register.TryRegister(typeof(IClassMultiple), typeof(ClassMultiple1), ServiceLifetime.Transient, RegistrationCompareMode.ServiceTypeAndImplementationType);
        register.TryRegister(typeof(IClassMultiple), typeof(ClassMultiple2), ServiceLifetime.Transient, RegistrationCompareMode.ServiceTypeAndImplementationType);

        register.TryRegister<IClassMultiple, ClassMultiple1>(p => new ClassMultiple1(), ServiceLifetime.Transient, RegistrationCompareMode.ServiceTypeAndImplementationType);
        register.TryRegister<IClassMultiple, ClassMultiple2>(p => new ClassMultiple2(), ServiceLifetime.Transient, RegistrationCompareMode.ServiceTypeAndImplementationType);

        register.TryRegister<IClassMultiple>(new ClassMultiple1(), RegistrationCompareMode.ServiceTypeAndImplementationType);
        register.TryRegister<IClassMultiple>(new ClassMultiple2(), RegistrationCompareMode.ServiceTypeAndImplementationType);

        base.RegisterServices(register);
    }

    [Theory]
    [DependencyInjectionData]
    public void Registration_Should_Work(string container)
    {
        Schema.Initialize();
    }

    public class MySchema : Schema
    {
        public MySchema(
            IServiceProvider provider,
            IEnumerable<IClassSingle> single,
            IEnumerable<IClassMultiple> multiple)
            : base(provider)
        {
            var singleArray = single.ToArray();
            singleArray.Length.ShouldBe(1);
            singleArray[0].ShouldBeOfType<ClassSingle1>();

            var multipleArray = multiple.ToArray();
            multipleArray.Length.ShouldBe(2);
            multipleArray[0].ShouldBeOfType<ClassMultiple1>();
            multipleArray[1].ShouldBeOfType<ClassMultiple2>();
        }
    }

    public interface IClassSingle { }
    public class ClassSingle1 : IClassSingle
    {
    }
    public class ClassSingle2 : IClassSingle
    {
    }

    public interface IClassMultiple { }
    public class ClassMultiple1 : IClassMultiple
    {
    }
    public class ClassMultiple2 : IClassMultiple
    {
    }
}
