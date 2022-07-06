<template>
  <b-container>
    <b-row v-if="ordersLoaded" class="py-4">
      <b-col cols="1"></b-col>
      <b-col>
        <b-table
          cols="10"
          v-if="ordersLoaded"
          :fields="fields"
          :items="items"
          show-empty
        >
          <template #cell(id)="data">
            {{ data.item.id }}
          </template>
          <template #cell(bookName)="data">
            {{ data.item.bookName }}
          </template>
          <template #cell(quantity)="data">
            {{ data.item.quantity }}
          </template>
          <template #cell(totalPrice)="data">
            {{ data.item.totalPrice }}
          </template>
          <template #cell(cancel)="data">
            <b-button
              variant="danger"
              @click="openOrderCancellationForm(data.item)"
              :disabled="
                data.item.status == 'Shipped to customer' ||
                data.item.status == 'Canceled' ||
                data.item.status == 'Awaiting payment'
              "
              >Anuluj</b-button
            >
          </template>
          <template #cell(confirm)="data">
            <b-button
              variant="success"
              @click="openOrderConfirmationForm(data.item)"
              :disabled="
                data.item.status == 'Shipped to customer' ||
                data.item.status == 'Canceled' ||
                data.item.status == 'Awaiting payment'
              "
              >Potwierdź</b-button
            >
          </template>
          <template #empty="">
            <h4><b>No orders found</b></h4>
          </template>
        </b-table>
      </b-col>
      <b-col cols="1"></b-col>
    </b-row>
    <b-row v-else>
      <b-col cols="12" class="py-4">
        <b-spinner style="width: 3rem; height: 3rem" />
      </b-col>
    </b-row>
    <b-modal id="confirm-order-modal" size="md" :hide-header="true">
      <b-container>
        <h4>Confirm Your order</h4>
      </b-container>
      <template #modal-footer="{}">
        <b-button variant="danger" @click="closeOrderConfirmationForm()">
          Cancel
        </b-button>
        <b-button variant="success" @click="confirmOrder()"
          >Confirm order</b-button
        >
      </template>
    </b-modal>
    <b-modal id="cancel-order-modal" size="md" :hide-header="true">
      <h4>Are You sure You want to cancel Your order?</h4>
      <template #modal-footer="{}">
        <b-button variant="danger" @click="closeOrderCancellationForm()">
          No, I want to keep it
        </b-button>
        <b-button variant="success" @click="cancelOrder()"
          >Cancel order</b-button
        >
      </template>
    </b-modal>
  </b-container>
</template>

<script>
import {
  getOrderStatus,
  getBooksPrices,
  getBooksDiscounts,
  confirmOrder,
  cancelOrder,
} from "@/api/api.js";

const ORDER_FIELDS = [
  "ID",
  "bookName",
  "quantity",
  "totalPrice",
  "status",
  "cancel",
  "confirm",
];

export default {
  name: "Orders",
  data: function () {
    return {
      orders: [],
      items: [],
      fields: ORDER_FIELDS,
      ordersLoaded: false,
      orderToConfirm: null,
    };
  },

  mounted() {
    this.parseOrders();
  },

  methods: {
    async parseOrders() {
      this.ordersLoaded = false;
      this.orders = this.$orders;
      this.items = [];

      for (let order of this.orders) {
        let statusReq = await getOrderStatus(order.orderId);
        let parsed_order = {
          id: order.orderId,
          bookName: order.book.name,
          bookId: order.book.id,
          quantity: order.quantity,
          totalPrice: order.quantity * order.book.unitPrice,
          unitPrice: order.book.unitPrice,
          discount: order.book.discount,
          status: statusReq.status,
        };
        this.items.push(JSON.parse(JSON.stringify(parsed_order)));
      }

      this.ordersLoaded = true;
      this.watchForStatusChange();
    },

    async watchForStatusChange() {
      setInterval(() => {
        for (let order of this.orders) {
          getOrderStatus(order.orderId).then((status) => {
            let index = this.orders.indexOf(order);
            this.items[index].status = status.status;

            if (this.items[index].status == "Canceled" && this.orderToConfirm.id == order.orderId) {
              this.closeOrderConfirmationForm();
              this.closeOrderCancellationForm();
            }
          });
        }
      }, 3000);
    },

    openOrderConfirmationForm(order) {
      this.orderToConfirm = order;

      this.$bvModal.show("confirm-order-modal");
    },

    closeOrderConfirmationForm() {
      this.$bvModal.hide("confirm-order-modal");
    },

    openOrderCancellationForm(order) {
      this.orderToConfirm = order;

      this.$bvModal.show("cancel-order-modal");
    },

    closeOrderCancellationForm() {
      this.$bvModal.hide("cancel-order-modal");
    },

    async confirmOrder() {
      // TODO: Add logic about confirming order

      let res = await confirmOrder(this.orderToConfirm);

      if (res.status === 200) {
        this.$confirmedOrders.push(this.orderToConfirm);
        this.$bvToast.toast(`Successfully confirmed order!`, {
          title: "Order confirmation",
          autoHideDelay: 5000,
          appendToast: true,
        });
      } else {
        this.$bvToast.toast(`Failed to confirm order!`, {
          title: "Order confirmation",
          autoHideDelay: 5000,
          appendToast: true,
        });
      }

      this.closeOrderConfirmationForm();
    },

    async cancelOrder() {
      // TODO: Add logic about confirming order

      let res = await cancelOrder(this.orderToConfirm);

      if (res.status === 200) {
        getOrderStatus(this.orderToConfirm.id).then((status) => {
          let index = this.orders.indexOf(this.orderToConfirm);
          this.items[index].status = status.status;
        });
        this.$bvToast.toast(`Successfully cancelled order!`, {
          title: "Order cancellation",
          autoHideDelay: 5000,
          appendToast: true,
        });
      } else {
        this.$bvToast.toast(`Failed to cancel order!`, {
          title: "Order cancellation",
          autoHideDelay: 5000,
          appendToast: true,
        });
      }

      this.closeOrderCancellationForm();
    },

    // addToBasket(bookID) {
    //   this.basket.push({
    //     book: book,
    //     quantity: this.form.quantity,
    //   });
    //   this.form.quantity = null;
    //   this.$bvModal.hide("book-reservation-modal");
    // },

    // async viewUser(row) {
    //   await this.loadUser(row.item);
    //   row.toggleDetails();
    // },

    // async loadUser(userObject) {
    //   let { name, surname, email, birthdate } = await getUser(userObject.login);

    //   userObject.name = name;
    //   userObject.surname = surname;
    //   userObject.email = email;
    //   userObject.birthdate = birthdate;
    //   userObject.detailsLoaded = true;
    // },

    // async deleteUser(login) {
    //   await apiDeleteUser(login);
    //   this.parseUsers();
    // },

    // resetCreateForm() {
    //   for (let key in this.form) {
    //     this.form[key] = null;
    //   }
    // },

    // resetEditForm() {
    //   for (let key in this.userToEdit) {
    //     this.form[key] = null;
    //   }
    // },

    // async createUser() {
    //   console.log(this.form);
    //   await apiCreateUser(this.form);
    //   this.resetCreateForm();
    //   this.parseUsers();
    //   this.closeCreateUserForm();
    // },
  },
};
</script>

<style lang="scss" scoped></style>
