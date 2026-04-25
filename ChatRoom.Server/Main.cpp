#include <iostream>
#include <boost/asio.hpp>
#include "CoreService.h"

int main()
{
	CoreService coreService(tcp::endpoint(tcp::v4(), 12345));
	coreService.StartCoreService();
}
